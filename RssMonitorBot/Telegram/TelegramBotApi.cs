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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private HttpClient _httpClient = null;

        private string _apiKey;

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

        public TelegramBotApi(string apiKey, TimeSpan httpTimeout)
        {
            _apiKey = apiKey;

            BaseUriForGetUpdates = BaseUriForMethod(MethodName_GetUpdates, apiKey);
            BaseUriForGetMe = BaseUriForMethod(MethodName_GetMe, apiKey);
            BaseUriForSendMessage = BaseUriForMethod(MethodName_SendMessage, apiKey);
            BaseUriForSendContact = BaseUriForMethod(MethodName_SendContact, apiKey);
            BaseUriForGetFile = BaseUriForMethod(MethodName_GetFile, apiKey);

            _httpClient = new HttpClient();
            _httpClient.Timeout = httpTimeout;
            _httpClient.BaseAddress = new Uri(BaseTelegramUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Mime_ApplicationJson));
        }

        private async Task<TResult> DoGetMethodCall<TResult>(string uri)
            where TResult: class 
        {
            logger.Debug("Executing method call {0}", uri.Replace(_apiKey, "--api-key--"));

            TResult ret = null;
            try
            {
                string resp = await _httpClient.GetStringAsync(uri);
                
                if (!string.IsNullOrEmpty(resp))
                {
                    logger.Debug("Raw result: {0}", resp);
                    ret = CallResult<TResult>.FromJsonString(resp)?.Result ?? null;
                }
                else 
                {
                    logger.Warn("Got an empty result from the server");
                }

            }
            catch (HttpRequestException e)
            {
                logger.Warn(e, "Http Request Timeout");
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
            logger.Info($"Getting updates: {offset} {limit} {timeout}");

            var ub = new UriBuilder(BaseUriForGetUpdates);
            if (offset != null)
            {
                ub.AddArgument(nameof(offset), offset.Value);
            }
            if (limit != null)
            {
                ub.AddArgument(nameof(limit), limit.Value);
            }
            if (timeout != null)
            {
                ub.AddArgument(nameof(timeout), timeout.Value);
            }
            if (allowed_updates != null)
            {
                ub.AddArgument(nameof(allowed_updates), JsonConvert.SerializeObject(allowed_updates));
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
            logger.Info($"Sending message to chat id {chat_id}");

            var ub = new UriBuilder(BaseUriForSendMessage);
            ub.AddArgument(nameof(chat_id), chat_id);
            ub.AddArgument(nameof(text), text);

            if (parse_mode != null)
            {
                ub.AddArgument(nameof(parse_mode), parse_mode);
            }
            if (disable_web_page_preview != null)
            {
                ub.AddArgument(nameof(disable_web_page_preview), disable_web_page_preview.Value ? "true" : "false");
            }
            if (disable_notification != null)
            {
                ub.AddArgument(nameof(disable_notification), disable_notification.Value ? "true" : "false");
            }
            if (reply_to_message_id != null)
            {
                ub.AddArgument(nameof(reply_to_message_id), reply_to_message_id.Value);
            }
            if (reply_markup != null)
            {
                ub.AddArgument(nameof(reply_markup), reply_markup);
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
