using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram;

namespace RssMonitorBot.Telegram
{
    public class RssTelegramBot : TelegramBotCore
    {
        public RssTelegramBot(ITelegramBotApi api, int numWorkers) : base(api, numWorkers)
        {
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

            if (UserState<AuthValidFlag>.ExistsFor(userId) && UserState<AuthValidFlag>.LoadOrDefault(userId).Data.AuthValid)
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
                UserState<AuthValidFlag>.LoadOrDefault(userId).Save();
                var r = await api.RespondToUpdate(update, $"{from.FirstName}, you are now authenticated");
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

/word <number> [optional] [keywords] 
    - update the list of keywords for subscription index, keywords can be empty

/mute 
    - mute everything, you would keep receiving updates but without any notifications

/unmute 
    - unmute everything

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

            state.Data.RssEntries.Add(
                new RssUrlEntry
                {
                    Url = parsedCommand[1], 
                    Keywords = parsedCommand.Skip(2).ToArray()
                });

            state.Save();

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
                    var kws = string.Join(" ", entry.Keywords);
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

            state.Data.RssEntries[idx].Keywords = parsedCommand.Skip(2).ToArray();
            state.Save();

            var r = await api.RespondToUpdate(update, $"{from.FirstName}, {idx} was updated");
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


    }
}
