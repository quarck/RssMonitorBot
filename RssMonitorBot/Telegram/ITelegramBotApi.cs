using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Telegram
{
    public interface ITelegramBotApi
    {
        Task<User> GetMe();

        Task<List<Update>> GetUpdates(
            long? offset = null,
            long? limit = null,
            long? timeout = null, // in seconds
            List<String> allowed_updates = null
            );

        Task<Message> SendMessage(
            string chat_id, // required, can be integer 
            string text, // required 
            string parse_mode = null,
            bool? disable_web_page_preview = null,
            bool? disable_notification = null,
            long? reply_to_message_id = null,
            string reply_markup = null);

        Task<Message> RespondToUpdate(
            Update update,
            string text, // required 
            string parse_mode = null,
            bool? disable_web_page_preview = null,
            bool? disable_notification = null,
            bool quote_original_message = false,
            string reply_markup = null);
    }
}
