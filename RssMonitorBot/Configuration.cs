using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace RssMonitorBot
{
    static class Configuration
    {
        public static string API_KEY = ApiKeys.TELEGRAM_API_KEY;
        public static string SERVER_ROOT {
            get
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 2)
                {
                    Console.Error.WriteLine("Error: Give a server root at arg1");
                    Environment.Exit(-1);
                }

                return args[1];
            } 
        }
    }
}
