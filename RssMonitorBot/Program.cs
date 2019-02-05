using RssMonitorBot.Telegram;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RssMonitorBot
{
    class Program
    {
        static void Main()
        {
            var client = new HttpClient();
            //var botApi = new TelegramBotApi(client, Configuration.API_KEY);
            //var bot = new TelegramBot(botApi, 4);
            //bot.Start();
            //bot.WaitAny();

            var reader = new RssReader(client);

            var readTask = reader.FetchAndParse("https://www.rte.ie/news/rss/news-headlines.xml");
            readTask.Wait();
            var res = readTask.Result;

            Console.WriteLine($"Done? {res.ToString()}");

            Console.ReadLine();
        }
    }
}
