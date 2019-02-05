using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Telegram
{
    public abstract class TelegramBotCore
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private ITelegramBotApi _api;

        public int CallTimeout { get; set; } = 900;
        public int CallNumUpdatesLimit { get; set; } = 100;

        public int QueueMaxSize { get; set; } = 1000;

        public int NumWorkers { get; private set; }

        private Queue<Update> _queue;
        private object _queueLock;
        private SemaphoreSlim _queueCount;

        public Task[] _taskArray;
        public object _taskArrayLock;

        public TelegramBotCore(ITelegramBotApi api, int numWorkers=10)
        {
            _api = api;
            _queue = new Queue<Update>();
            _queueLock = new object();
            _queueCount = new SemaphoreSlim(0);

            NumWorkers = numWorkers;
            _taskArrayLock = new object();
        }

        internal ITelegramBotApi API => _api;

        public void StartAsync()
        {
            _taskArray = new Task[NumWorkers + 1];

            _taskArray[0] = UpdateReceiveLoop();

            for (int i = 0; i < NumWorkers; ++i)
            {
                _taskArray[i+1] = Worker();
            }
        }

        private async Task UpdateReceiveLoop()
        {
            await Task.Yield();

            try
            {
                var me = await _api.GetMe();
                if (me != null)
                {
                    logger.Info("Bot self identification: {0}", me.ToJsonString());
                }

                long? nextOffset = null;

                for (; ; )
                {
                    try
                    {
                        var updates = await _api.GetUpdates(
                            offset: nextOffset,
                            timeout: CallTimeout,
                            limit: CallNumUpdatesLimit
                            );

                        if (updates != null && updates.Count > 0)
                        {
                            logger.Info($"Got {updates.Count} updates from the network");

                            foreach (var update in updates)
                            {
                                nextOffset = update.UpdateId + 1;
                                if (update.Message == null)
                                    continue;

                                User from = update.Message.From;
                                long userId = update.Message.From.Id;
                                string text = update.Message.Text ?? "";

                                logger.Info($"{from.ToString()}: {userId} {text}");
                                logger.Debug("full update: {0}", update.ToJsonString());

                                bool added = false;
                                lock (_queueLock)
                                {
                                    if (_queue.Count < QueueMaxSize)
                                    {
                                        _queue.Enqueue(update);
                                        _queueCount.Release(1);
                                        added = true;
                                    }
                                }

                                if (!added)
                                {
                                    logger.Error($"Queue overflow, update dropped");
                                    var msg = await _api.RespondToUpdate(update, "Bot internal queue overflow");
                                }
                            }
                        }
                        else
                        {
                            logger.Info("No updates");
                        }

                        await Task.Delay(100);
                    }
                    catch (TaskCanceledException ex)
                    {
                        logger.Error(ex, "Read task cancelled");
                        await Task.Delay(10 * 1000);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Telegram update loop got an exception");
                throw;
            }
        }

        public async Task Worker()
        {
            await Task.Yield();

            logger.Info("worker started");

            for(;;)
            {
                Update update = null;

                await _queueCount.WaitAsync();

                lock (_queueLock)
                {
                    if (_queue.Count == 0)
                    {
                        logger.Error("internal error: _queue and _queueCount didn't match");
                        Environment.Exit(-1);
                    }

                    update = _queue.Dequeue();
                }

                if (update != null)
                {
                    logger.Info($"worker got an update from {update?.Message?.From} - processing");
                    try
                    {
                        HandleUserMessage(_api, update);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "HandlerUserMessage has thrown an exception - terminating");
                        throw;
                    }
                }
            }
        }

        internal abstract void HandleUserMessage(ITelegramBotApi api, Update msg);

        public void Wait()
        {
            Task.WaitAny(_taskArray);
        }
    }
}
