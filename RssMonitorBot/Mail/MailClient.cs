using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace RssMonitorBot.Mail
{
    public static class MailClient
    {
        public static async Task<bool> SendMail(string to, string subject, string body, bool isHtmlFormatted=false)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/sendmail",
                    Arguments = "-t",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                }
            };

            await Task.Factory.StartNew(() => process.Start());

            System.IO.StreamWriter stdin = process.StandardInput;
            await stdin.WriteAsync($"To: {to}\n");
            if (isHtmlFormatted)
                await stdin.WriteAsync("Content-Type: text/html\n");
            await stdin.WriteAsync($"Subject: {subject.Replace('\n', ' ')}\n");
            await stdin.WriteAsync("\n");
            await stdin.WriteAsync(body.Replace("\n.\n", "\n.-\n"));
            await stdin.WriteAsync("\n.\n");

            await Task.Factory.StartNew(() => process.WaitForExit());

            return process.ExitCode == 0;
        }
    }
}
