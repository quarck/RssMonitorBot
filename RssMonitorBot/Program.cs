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
                FileNameKind = NLog.Targets.FilePathKind.Absolute
            };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            config.AddRule(LogLevel.Error, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = config;
        }

        static void Main()
        {
            var serverRoot = Configuration.SERVER_ROOT;

            SetupLogging(Path.Combine(serverRoot, "log.txt"));

            var botApi = new TelegramBotApi(Configuration.API_KEY);
            var bot = new RssTelegramBot(botApi, 4);
            bot.Start();
            bot.WaitAny();

            //var reader = new RssReader();

            //var readTask = reader.FetchAndParse("https://www.rte.ie/news/rss/news-headlines.xml");
            //readTask.Wait();
            //var res = readTask.Result;

            //Console.WriteLine($"Done? {res.ToString()}");

            //Console.ReadLine();
        }
    }
}
