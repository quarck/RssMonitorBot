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

        private static T GetArgNT<T>(int n, string name, Func<string, T> parse)
        {
            try
            {
                return parse(GetArgN(n, name));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error with arg {n} ({name}): {ex}");
                throw;
            }
        }

        public static string SERVER_ROOT = GetArgN(1, "server root");
        public static string API_KEY = GetArgN(2, "api key");
        public static string BOT_SECRET = GetArgN(3, "bot secret");
        public static int REFRESH_INTERVAL_SECONDS = GetArgNT<int>(4, "refresh interval", x => int.Parse(x));
        public static bool USE_EMAIL_NOTIFICATION = GetArgNT<bool>(5, "use email notification", x => bool.Parse(x));
        public static string NOTIFICATION_EMAIL_DST = GetArgN(6, "notification email dst");
    }
}
