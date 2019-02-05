// Online API manual: https://core.telegram.org/bots/api 

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Telegram
{
    class TelegramBotApi: ITelegramBotApi
    {
        private HttpClient _httpClient = null;

        private static string MethodName_GetUpdates = "getUpdates";
        private static string MethodName_GetMe = "getMe";
        private static string MethodName_SendMessage = "sendMessage";
        private static string MethodName_SendContact = "sendContact";
        private static string MethodName_GetFile = "getFile";

        private static string BaseTelegramUrl = "https://api.telegram.org/";

        private static string Mime_ApplicationJson = "application/json";

        private static string BaseUriForMethod(string method, string apiKey) =>
            $"https://api.telegram.org/bot{apiKey}/{method}";

        public readonly string BaseUriForGetUpdates;
        public readonly string BaseUriForGetMe;
        public readonly string BaseUriForSendMessage;
        public readonly string BaseUriForSendContact;
        public readonly string BaseUriForGetFile;

        public bool VerboseLogging { get; set; } = false;

        public TelegramBotApi(HttpClient client, string apiKey)
        {
            BaseUriForGetUpdates = BaseUriForMethod(MethodName_GetUpdates, apiKey);
            BaseUriForGetMe = BaseUriForMethod(MethodName_GetMe, apiKey);
            BaseUriForSendMessage = BaseUriForMethod(MethodName_SendMessage, apiKey);
            BaseUriForSendContact = BaseUriForMethod(MethodName_SendContact, apiKey);
            BaseUriForGetFile = BaseUriForMethod(MethodName_GetFile, apiKey);

            _httpClient = client;
            _httpClient.BaseAddress = new Uri(BaseTelegramUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Mime_ApplicationJson));
        }

        private async Task<TResult> DoGetMethodCall<TResult>(string uri)
            where TResult: class 
        {
            if (VerboseLogging)
                Console.WriteLine($"Request: {uri}");

            TResult ret = null;
            try
            {
                string resp = await _httpClient.GetStringAsync(uri);
                
                if (!string.IsNullOrEmpty(resp))
                {
                    if (VerboseLogging)
                        Console.WriteLine($"Raw result: {resp}");
                    ret = CallResult<TResult>.FromJsonString(resp)?.Result ?? null;
                }
                else if (VerboseLogging)
                {
                    Console.WriteLine("Empty result");
                }

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.ToString());
            }

            return ret;
        }

        public async Task<User> GetMe()
        {
            return await DoGetMethodCall<Telegram.User>(BaseUriForGetMe);
        }

        public async Task<List<Update>> GetUpdates(
            long? offset = null, 
            long? limit = null, 
            long? timeout = null, // in seconds
            List<String> allowed_updates = null 
            )
        {
            var ub = new UriBuilder(BaseUriForGetUpdates);
            if (offset != null)
            {
                ub.AddArgument("offset", offset.Value);
            }
            if (limit != null)
            {
                ub.AddArgument("limit", limit.Value);
            }
            if (timeout != null)
            {
                ub.AddArgument("timeout", timeout.Value);
            }
            if (allowed_updates != null)
            {
                ub.AddArgument("allowed_updates", JsonConvert.SerializeObject(allowed_updates));
            }

            return await DoGetMethodCall<List<Telegram.Update>>(ub.ToString());
        }

        public async Task<Telegram.Message> SendMessage(
            string chat_id, // required, can be integer 
            string text, // required 
            string parse_mode = null, 
            bool? disable_web_page_preview = null, 
            bool? disable_notification = null, 
            long? reply_to_message_id = null, 
            string reply_markup = null)
        {
            var ub = new UriBuilder(BaseUriForSendMessage);
            ub.AddArgument("chat_id", chat_id);
            ub.AddArgument("text", text);

            if (parse_mode != null)
            {
                ub.AddArgument("parse_mode", parse_mode);
            }
            if (disable_web_page_preview != null)
            {
                ub.AddArgument("disable_web_page_preview", disable_web_page_preview.Value ? "true" : "false");
            }
            if (disable_notification != null)
            {
                ub.AddArgument("disable_notification", disable_notification.Value ? "true" : "false");
            }
            if (reply_to_message_id != null)
            {
                ub.AddArgument("reply_to_message_id", reply_to_message_id.Value);
            }
            if (reply_markup != null)
            {
                ub.AddArgument("reply_markup", reply_markup);
            }

            return await DoGetMethodCall<Telegram.Message>(ub.ToString());
        }

        public async Task<Message> RespondToUpdate(
            Update update,
            string text, // required 
            string parse_mode = null,
            bool? disable_web_page_preview = null,
            bool? disable_notification = null,
            bool quote_original_message = false,
            string reply_markup = null)
        {
            return await SendMessage(
                chat_id: update.Message.Chat.Id.ToString(), 
                text: text, 
                parse_mode: parse_mode, 
                disable_web_page_preview: disable_web_page_preview, 
                disable_notification: disable_notification, 
                reply_to_message_id: quote_original_message ? (long?)update.Message.MessageId : null,
                reply_markup: reply_markup);
        }
    }
}
