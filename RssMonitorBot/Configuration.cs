using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace RssMonitorBot
{
    static class Configuration
    {
        private static string GetArgN(int n, string name)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length < n+1)
            {
                Console.Error.WriteLine($"Error: Give a {name} at arg{n}");
                Environment.Exit(-1);
            }

            return args[n];
        }

        public static string SERVER_ROOT = GetArgN(1, "server root");
        public static string API_KEY = GetArgN(2, "api key");
        public static string BOT_SECRET = GetArgN(3, "bot secret");
    }
}
