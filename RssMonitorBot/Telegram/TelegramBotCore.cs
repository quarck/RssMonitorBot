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
        private ITelegramBotApi _api;

        public bool VerboseLogging { get; set; } = false;
        public int CallTimeout { get; set; } = 900;
        public int CallNumUpdatesLimit { get; set; } = 100;

        public int QueueMaxSize { get; set; } = 1000;

        public int NumWorkers { get; private set; }

        public Queue<Update> _queue;
        public object _queueLock;

        public Task[] _taskArray;

        public TelegramBotCore(ITelegramBotApi api, int numWorkers)
        {
            _api = api;
            _queue = new Queue<Telegram.Update>();
            _queueLock = new object();
            NumWorkers = numWorkers;
            _taskArray = new Task[NumWorkers + 1];
        }

        public void Start()
        {
            _taskArray[0] = Task.Factory.StartNew(
                () => {
                    try
                    {
                        RunReceiveLoop().Wait();
                        Console.WriteLine("Recv loop done");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"recv loop done: {ex}");
                    }
                });

            for (int i = 0; i < NumWorkers; ++i)
            {
                _taskArray[i+1] = Task.Factory.StartNew(
                    () => {
                        try
                        {
                            Worker();
                            Console.WriteLine("Worker done");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"worker died: {ex}");
                        }
                    });
            }
        }

        public async Task RunReceiveLoop()
        {
            var me = await _api.GetMe();
            if (me != null)
            {
                Console.WriteLine("me: {0}", me.ToJsonString());
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
                        foreach (var update in updates)
                        {
                            nextOffset = update.UpdateId + 1;
                            if (update.Message == null)
                                continue;

                            if (VerboseLogging)
                            {
                                Telegram.User from = update.Message.From;
                                long userId = update.Message.From.Id;
                                string text = update.Message.Text ?? "";
                                Console.WriteLine($"{from.ToString()}: {userId} {text}");
                            }

                            bool added = false;
                            lock (_queueLock)
                            {
                                if (_queue.Count < QueueMaxSize)
                                {
                                    _queue.Enqueue(update);
                                    Monitor.PulseAll(_queueLock);
                                    added = true;
                                }
                            }
                            
                            if (!added)
                            {
                                var msg = await _api.RespondToUpdate(update, "Bot internal queue overflow");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Updates");
                    }

                    await Task.Delay(100);
                }
                catch (TaskCanceledException ex)
                {
                    await Task.Delay(10 * 1000);
                }
            }
        }

        public void Worker()
        {
            for(;;)
            {
                Telegram.Update update = null;

                lock (_queueLock)
                {
                    if (_queue.Count == 0)
                    {
                        Monitor.Wait(_queueLock);
                    }

                    if (_queue.Count != 0)
                    {
                        update = _queue.Dequeue();
                    }
                }

                if (update != null)
                {
                    HandleUserMessage(_api, update);
                }
            }
        }

        internal abstract void HandleUserMessage(ITelegramBotApi api, Update msg);

        public void WaitAny()
        {
            Task.WaitAny(_taskArray);
        }
    }
}
