using NLog;
using RssMonitorBot.Telegram;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

namespace RssMonitorBot
{
    class Program
    {
        static void SetupLogging(string path)
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") {
                FileName = path,
                FileNameKind = NLog.Targets.FilePathKind.Absolute, 
                Layout = "${longdate} ${level:uppercase=true} ${logger}: ${message} ${exception:format=tostring}"
            };

            var logconsole = new NLog.Targets.ConsoleTarget("logconsole")
            {
                Layout = "${longdate} ${level:uppercase=true} ${logger}: ${message} ${exception:format=tostring}"
            };

#if DEBUG
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
#endif
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = config;
        }

        static void Main()
        {
            var serverRoot = Configuration.SERVER_ROOT;

            SetupLogging(Path.Combine(serverRoot, "log.txt"));

            var bot =
                new RssTelegramBot(
                    new TelegramBotApi(Configuration.API_KEY, TimeSpan.FromSeconds(1000)),
                    new RssReader(), 
                    refreshIntervalSeconds: Configuration.REFRESH_INTERVAL_SECONDS
                );

            // Eventually the whole bot would run on just two threads, one for rss fetching 
            // and another one for communication with telegram servers, 
            // all the other parallelerism is done via Task's cooperative parallelerism
            var tasks = new Task[2];
            tasks[0] = Task.Factory.StartNew(() => bot.StartAsync().Wait());
            tasks[1] = Task.Factory.StartNew(() => bot.StartRssWorkersAsync().Wait());

            // If any of the tasks above finishes - something has gone wrong, so we terminate 
            // on the any of the tasks termination
            Task.WaitAny(tasks);
        }
    }
}
