using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

namespace RssMonitorBot.Telegram
{
    public class RssTelegramBot : TelegramBotCore
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private IRssReader _rssReader;

        private Task[] _updateWorkers;
        private readonly int _numRssUpdateWorkers;
        private readonly TimeSpan _refreshInterval;

        public RssTelegramBot(ITelegramBotApi api, 
            IRssReader rssReader,
            int refreshIntervalSeconds = 600,
            int numWorkers=10, 
            int numRssUpdateWorkers=10
            ) 
            : base(api, numWorkers)
        {
            _refreshInterval = TimeSpan.FromSeconds(refreshIntervalSeconds);
            _numRssUpdateWorkers = numRssUpdateWorkers;
            _rssReader = rssReader;
        }

        public async Task StartRssWorkersAsync()
        {
            _updateWorkers = new Task[_numRssUpdateWorkers];
            for (int i = 0; i < _updateWorkers.Length; ++ i)
            {
                _updateWorkers[i] = UpdateWorkerAsync(i);
            }

            await Task.WhenAny(_updateWorkers);

            logger.Error("One of the workers terminated - shutting down");
            Environment.Exit(-1);
        }

        internal static string ParseKeyword(string kw)
        {
            return kw.Replace('_', ' ');
        }

        internal override void HandleUserMessage(ITelegramBotApi api, Update update)
        {
            HandleUserMessageAsync(api, update).Wait();
        }

        private async Task HandleUserMessageAsync(ITelegramBotApi api, Update update)
        {
            User from = update.Message.From;
            Chat chat = update.Message.Chat;
            long userId = update.Message.From.Id;
            string text = update.Message.Text ?? "";

            string[] commandItems = text.Split(' ');

            if (commandItems.Length < 1)
            {
                return;
            }

            if (UserState<UserDetails>.ExistsFor(userId) && UserState<UserDetails>.LoadOrDefault(userId).Data.AuthValid)
            {
                await HandleAuthenticatedUserCommand(api, update, from, chat, userId, commandItems);
            }
            else
            {
                await HandleNonAuthenticatedUserCommand(api, update, from, chat, userId, commandItems);
            }
        }

        private async Task HandleAuthenticatedUserCommand(
            ITelegramBotApi api,
            Update update, 
            User from, 
            Chat chat, 
            long userId, 
            string[] parsedCommand
            )
        {
            switch (parsedCommand[0])
            {
                case "/help":
                    await HandleAuthenticatedUserHelpCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/add":
                    await HandleAuthenticatedUserAddCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/list":
                    await HandleAuthenticatedUserListCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/del":
                    await HandleAuthenticatedUserDelCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/stop":
                    await HandleAuthenticatedUserStopCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/words":
                    await HandleAuthenticatedUserWordsCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/mute":
                    await HandleAuthenticatedUserMuteCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/unmute":
                    await HandleAuthenticatedUserUnmuteCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                case "/hours":
                    await HandleAuthenticatedUserHoursCommand(api, update, from, chat, userId, parsedCommand);
                    break;

                default:
                    await HandleAuthenticatedUserUnknownCommand(api, update, from, chat, userId, parsedCommand);
                    break;
            }
        }

        private async Task HandleNonAuthenticatedUserCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            if (parsedCommand.Length == 2 && 
                parsedCommand[0] == "/auth" && 
                parsedCommand[1] == Configuration.BOT_SECRET)
            {
                var state = UserState<UserDetails>.LoadOrDefault(userId);
                state.Data.ChatId = chat.Id;
                state.Data.UserId = userId; // kind of overkill
                state.Save();

                var r = await api.RespondToUpdate(update, $"{from.FirstName}, you are now authenticated");
            }
            else if (parsedCommand.Length > 1 && parsedCommand[0] == "/start")
            {
                await HandleUnauthenticatedUserStartCommand(api, update, from, chat, userId, parsedCommand);
            }
            else
            {
                var r = await api.RespondToUpdate(update, $"{from.FirstName}, access denied");
            }
            
        }

        private async Task HandleAuthenticatedUserHelpCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var r = await api.RespondToUpdate(update,
                $@"Hello {from.FirstName}, here are the commands I can understand: 

/add <rss_url> [optional] [keywords]
    - subscribe to the rss feed, with optional keywords to filter by

/list 
    - list current subscriptions 

/del <number> 
    - delete subscription by the number 

/words <number> [optional] [keywords] 
    - update the list of keywords for subscription index, keywords can be empty

/words add <number> <word> 
    - add a new keyword to subscription index

/words del <number> <word> 
    - del a keyword from subscription index

/mute 
    - mute everything, you would keep receiving updates but without any notifications

/unmute 
    - unmute everything

/hours <from> <to>
    - set allowed notification hours, in 24 format, inclusive both, e.g. /hours 7 20 would 
    mean notifications are allowed from 7:00:00 to 20:00:00

/stop 
    - stop the bot completely, do any edit to un-stop it

GDPR PRIVACY NOTICE: 
There is no privacy. Consider anything you send to this bot as public.
");
        }

        private void UnStop(long userId)
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);
            if (state.Data.Stopped)
            {
                state.Data.Stopped = false;
                state.Save();
            }
        }

        private async Task HandleAuthenticatedUserAddCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            UnStop(userId);

            if (parsedCommand.Length < 2)
            {
                var rr = await api.RespondToUpdate(update, $"{from.FirstName}, please give arguments to the command");
                return;
            }

            var state = UserState<UserRssSubscriptions>.LoadOrDefault(userId);
            if (state.Data.RssEntries == null)
            {
                state.Data.RssEntries = new List<RssUrlEntry>();
            }

            var url = parsedCommand[1];
            var keyboards = parsedCommand.Skip(2).Select(x => ParseKeyword(x)).ToArray();

            if (state.Data.RssEntries.Find(x => x.Url == url) != null)
            {
                var re = await api.RespondToUpdate(update, $"{from.FirstName}, the URI {url} is already in your list");
                return;
            }

            var rssParsed = await _rssReader.FetchAndParse(url);
            if (rssParsed == null)
            {
                var re = await api.RespondToUpdate(update, $"{from.FirstName}, the URI {url} is not looking like a valid RSS");
                return;
            }

            state.Data.RssEntries.Add( new RssUrlEntry { Url = url, Keywords = keyboards } );

            state.Save();

            var rssPubDates = UserState<UserFeedPubDates>.LoadOrDefault(userId);
            if (rssPubDates.Data.PubDates == null)
            {
                rssPubDates.Data.PubDates = new SerializableDictionary<string, DateTime>();
            }
            rssPubDates.Data.PubDates[url] = rssParsed.LastBuildDate;

            try
            {
                rssPubDates.Save();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception while saving xml");
            }

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, it was added");
        }

        private async Task HandleAuthenticatedUserListCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            UnStop(userId);

            var state = UserState<UserRssSubscriptions>.LoadOrDefault(userId);
            var r = await api.RespondToUpdate(update, $"{from.FirstName}, here are your subscribtions:");

            if (state.Data.RssEntries != null)
            {
                for (int i = 0; i < state.Data.RssEntries.Count; ++i)
                {
                    var entry = state.Data.RssEntries[i];
                    var kws = string.Join(", ", entry.Keywords);
                    var rr = await api.RespondToUpdate(update, $"{i}: {entry.Url} {kws}");
                }
            }
        }


        private async Task HandleAuthenticatedUserDelCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            UnStop(userId);

            if (parsedCommand.Length < 2 || !int.TryParse(parsedCommand[1], out var idx))
            {
                var rr = await api.RespondToUpdate(update, $"{from.FirstName}, please give arguments to the command");
                return;
            }

            var state = UserState<UserRssSubscriptions>.LoadOrDefault(userId);

            if (state.Data.RssEntries == null || idx >= state.Data.RssEntries.Count || idx < 0)
            {
                var rr = await api.RespondToUpdate(update, $"{from.FirstName}, index {idx} is not known");
                return;
            }

            state.Data.RssEntries.RemoveAt(idx);
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, {idx} was removed");
        }

        private async Task HandleUnauthenticatedUserStartCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);
            state.Data.Stopped = true;
            state.Save();

            var r = await api.RespondToUpdate(update, $"Saluton {from.FirstName}. Estas propra boto, vi bezonas rajto pro uzi gxin");
        }


        private async Task HandleAuthenticatedUserStopCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);
            state.Data.Stopped = true;
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, bot won't bother you");
        }

        private async Task HandleAuthenticatedUserWordsCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            UnStop(userId);

            if (parsedCommand.Length < 2)
            {
                var rr = await api.RespondToUpdate(update, $"{from.FirstName}, please give arguments to the command");
                return;
            }

            var state = UserState<UserRssSubscriptions>.LoadOrDefault(userId);

            if (int.TryParse(parsedCommand[1], out var idx))
            {
                if (state.Data.RssEntries == null || idx >= state.Data.RssEntries.Count || idx < 0)
                {
                    var rr = await api.RespondToUpdate(update, $"{from.FirstName}, index {idx} is not known");
                    return;
                }

                var keyWords = parsedCommand.Skip(2).Select(x => ParseKeyword(x)).ToHashSet();
                state.Data.RssEntries[idx].Keywords = keyWords.ToArray();
                state.Save();

                var r = await api.RespondToUpdate(update, $"{from.FirstName}, {idx} was updated");
            }
            else if (parsedCommand[1] == "add" && parsedCommand.Length >= 4 && 
                int.TryParse(parsedCommand[2], out var addIdx))
            {                
                var addWords = parsedCommand.Select(x => ParseKeyword(x)).Skip(3);

                if (state.Data.RssEntries == null || addIdx >= state.Data.RssEntries.Count || addIdx < 0)
                {
                    var rr = await api.RespondToUpdate(update, $"{from.FirstName}, index {addIdx} is not known");
                    return;
                }

                if (state.Data.RssEntries[idx].Keywords == null)
                {
                    state.Data.RssEntries[idx].Keywords = addWords.ToArray();
                }
                else
                {
                    var words = state.Data.RssEntries[idx].Keywords.ToHashSet();
                    foreach (var newWord in addWords)
                    {
                        words.Add(newWord);
                    }
                    state.Data.RssEntries[idx].Keywords = words.ToArray();
                }

                state.Save();

                var r = await api.RespondToUpdate(update, $"{from.FirstName}, {idx} was updated");
            }
            else if (parsedCommand[1] == "del" && parsedCommand.Length >= 4 &&
                int.TryParse(parsedCommand[2], out var delIdx))
            {
                var delWords = parsedCommand.Select(x => ParseKeyword(x)).Skip(3);

                if (state.Data.RssEntries == null || delIdx >= state.Data.RssEntries.Count || delIdx < 0)
                {
                    var rr = await api.RespondToUpdate(update, $"{from.FirstName}, index {delIdx} is not known");
                    return;
                }

                if (state.Data.RssEntries[idx].Keywords != null)
                {
                    var words = state.Data.RssEntries[idx].Keywords.ToHashSet();
                    foreach (var delWord in delWords)
                    {
                        words.Remove(delWord);
                    }
                    state.Data.RssEntries[idx].Keywords = words.ToArray();
                }

                state.Save();

                var note = (state.Data.RssEntries[idx].Keywords == null ||
                    state.Data.RssEntries[idx].Keywords.Length == 0) 
                        ? ", WARNING: keyword list is not empty"
                        : "";

                var r = await api.RespondToUpdate(update, $"{from.FirstName}, {idx} was updated{note}");
            }
            else
            {
                var rr = await api.RespondToUpdate(update, $"{from.FirstName}, please give arguments to the command");
                return;
            }
        }

        private async Task HandleAuthenticatedUserMuteCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);
            state.Data.Muted = true;
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, bot muted");
        }

        private async Task HandleAuthenticatedUserUnmuteCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);
            state.Data.Muted = false;
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, bot un-muted");
        }

        private async Task HandleAuthenticatedUserHoursCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var state = UserState<UserMuteState>.LoadOrDefault(userId);

            if (parsedCommand.Length != 3 ||
                !int.TryParse(parsedCommand[1], out var hoursFrom) ||
                !int.TryParse(parsedCommand[2], out var hoursTo))
            {
                var r1 = await api.RespondToUpdate(update, $"{from.FirstName}, I need arguments");
                return;
            }

            state.Data.SetHours(hoursFrom, hoursTo);
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, bot will mute notifications when outside [{hoursFrom}:00:00, {hoursTo}:00:00]");
        }

        private async Task HandleAuthenticatedUserUnknownCommand(
            ITelegramBotApi api,
            Update update,
            User from,
            Chat chat,
            long userId,
            string[] parsedCommand
            )
        {
            var r = await api.RespondToUpdate(update,
                $"Hello {from.FirstName}, I cannot understand {parsedCommand[0]}, try asking for /help");
        }


        private async Task UpdateWorkerAsync(int index)
        {
            for (;;)
            {
                DateTime lastStart = DateTime.Now;

                IEnumerable<UserState<UserDetails>> users = 
                    UserState<UserDetails>.EnumerateAll(x => (x % _numRssUpdateWorkers) == index);

                foreach (var user in users)
                {
                    try
                    {
                        if (!user.Data.AuthValid)
                        {
                            logger.Info($"User {user.Data.UserId} is skipped - auth is not valid");
                            continue;
                        }

                        logger.Info($"Fetching feeds for user {user.Data.UserId}");
                        await RunFetcherAsync(user.Data);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "failed to run user data");
                    }
                }

                var nextStart = lastStart + _refreshInterval;
                var now = DateTime.Now;
                if (nextStart < now)
                {
                    logger.Error($"Bot seems overloaded: last update cycle took over {_refreshInterval}");
                    continue;
                }

                var toSleepMillis = (int)(nextStart - now).TotalMilliseconds;
                logger.Info($"Worker {index}: sleeping for {toSleepMillis}ms");
                await Task.Delay(toSleepMillis);
            }
        }

        private async Task RunFetcherAsync(UserDetails user)
        {
            var rssDetails = UserState<UserRssSubscriptions>.LoadOrDefault(user.UserId);
            var rssPubDates = UserState<UserFeedPubDates>.LoadOrDefault(user.UserId);

            var muteSettings = UserState<UserMuteState>.LoadOrDefault(user.UserId);

            if (muteSettings.Data.Stopped)
            {
                logger.Info($"Bot is stopped for {user.UserId} -- skipping");
                return;
            }

            var now = DateTime.Now;
            bool isMuted = muteSettings.Data.IsMutedNow(ref now);

            logger.Info($"User {user.UserId} has {rssDetails.Data.RssEntries?.Count} feeds");

            if ((rssDetails.Data.RssEntries?.Count ?? 0) == 0)
            {
                return;
            }

            var fetchAndParseTasks =
                rssDetails.Data
                    .RssEntries
                    .Select(feed => _rssReader.FetchAndParse(feed.Url))
                    .ToArray();

            RssFeed[] results = await Task.WhenAll(fetchAndParseTasks);

            if (rssPubDates.Data.PubDates == null)
            {
                rssPubDates.Data.PubDates = new SerializableDictionary<string, DateTime>();
            }

            for (int feedIdx = 0; feedIdx < results.Length; ++ feedIdx)
            {
                var feedInfo = rssDetails.Data.RssEntries[feedIdx];
                var feedData = results[feedIdx];

                if (feedData == null)
                {
                    logger.Info($"User {user.UserId}, feed {feedInfo.Url}: fetch/parse failed");
                    continue;
                }

                if (rssPubDates.Data.PubDates.TryGetValue(feedInfo.Url, out var prevPubDate))
                {
                    if (prevPubDate == feedData.LastBuildDate && prevPubDate != DateTime.MinValue)
                    {
                        logger.Info($"User {user.UserId}, feed {feedInfo.Url}: Feed didnt update");
                        continue;
                    }
                }
                else
                {
                    prevPubDate = DateTime.MinValue;
                }

                logger.Info($"User {user.UserId}, feed {feedInfo.Url}: Feed did update, new pub date: {feedData.LastBuildDate}");

                rssPubDates.Data.PubDates[feedInfo.Url] = feedData.LastBuildDate;

                foreach (var item in feedData.Items)
                {
                    if (item.PublicationDate <= prevPubDate && prevPubDate != DateTime.MinValue)
                    {
                        continue; // skipping the old item
                    }

                    logger.Info($"User {user.UserId}, feed {feedInfo.Url}: new feed item: {item.Title}, {item.Link}");

                    if (rssPubDates.Data.IsRecent(item.Link))
                    {
                        logger.Info($"User {user.UserId}, feed {feedInfo.Url}: user has seen this recently, skipping: {item.Link}");
                        continue;
                    }

                    bool hasKeywords = feedInfo.Keywords.Length == 0;

                    foreach (var kw in feedInfo.Keywords)
                    {
                        if (item.Title.Contains(kw, StringComparison.InvariantCultureIgnoreCase))
                        {
                            hasKeywords = true;
                            break;
                        }
                        else if (item.Description.Contains(kw, StringComparison.InvariantCultureIgnoreCase))
                        {
                            hasKeywords = true;
                            break;
                        }
                    }

                    if (hasKeywords)
                    {
                        rssPubDates.Data.AddRecentLink(item.Link);
                        await SendRssItem(user, item, isMuted);
                    }
                }
            }

            rssPubDates.Save();
        }

        private async Task SendRssItem(UserDetails user, RssFeedItem item, bool isMuted)
        {
            var message = $"[{item.Title}]({item.Link})";

            await API.SendMessage(
                user.ChatId.ToString(),
                message,
                disable_notification: isMuted ? (bool?)true : null, 
                disable_web_page_preview: false, 
                parse_mode: "Markdown" 
                );
        }
    }
}
