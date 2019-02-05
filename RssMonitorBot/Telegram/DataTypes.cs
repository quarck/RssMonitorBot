using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Telegram
{
    internal static class JsonSettings
    {
        internal static JsonSerializerSettings IgnoreUnknowns =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
    }

    public class User
    {
        public long Id { get; set; }
        public bool IsBot { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; } // Optional 
        public string Username { get; set; } // Optional 
        public string LanguageCode { get; set; } // Optional 

        public override string ToString()
        {
            string bot = IsBot ? "Bot:" : "";
            return $"{bot}{Id}, {FirstName ?? ""} {LastName ?? ""} ({Username ?? ""})";
        }

        public static User FromJsonString(string json)
        {
            User ret = null;

            try
            {
                ret = JsonConvert.DeserializeObject<User>(json, JsonSettings.IgnoreUnknowns);
            }
            catch (JsonSerializationException ex)
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger?.Error(ex, $"Failed to parse Json: {json}");
            }
            return ret;
        }

        public string ToJsonString()
        {
            return JsonConvert.SerializeObject(this, JsonSettings.IgnoreUnknowns);
        }    
    }

    public class CallResult<T>
    {
        public bool Ok;
        public T Result;

        public static CallResult<T> FromJsonString(string json)
        {
            CallResult<T> ret = null;

            try
            {
                ret = JsonConvert.DeserializeObject<CallResult<T>>(json, JsonSettings.IgnoreUnknowns);
            }
            catch (JsonSerializationException ex)
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger?.Error(ex, $"Failed to parse Json: {json}");
            }
            return ret;
        }
    }

    public class ChatPhoto
    {
        public string SmallFileId;
        public string BigFileId;
    }

    public class Chat
    {
        public long Id;
        public string Type;
        public string Title;
        public string Username;
        public string FirstName;
        public string LastName;
        public bool AllMembersAreAdministrators;
        public ChatPhoto Photo;
        public string Description;
        public string InviteLink;
        public Message PinnedMessage;
        public string StickerSetName;
        public bool CanSetStickerSet;
    }

    public class MessageEntity
    {
        public string Type;
        public long Offset;
        public long Length;
        public string Url;
        public User User;
    }

    public class PhotoSize
    {
        public string FieldId;
        public long Width;
        public long Height;
        public long FileSize;
    }

    public class Audio
    {
        public string FielId;
        public long Duration;
        public string Performer;
        public string Title;
        public string Mimetype;
        public long FileSize;
        public PhotoSize Thumb;
    }

    public class Document
    {
        public string FileId;
        public PhotoSize Thumb;
        public string FileName;
        public string MimeType;
        public long FileSize;
    }

    public class Video
    {
        public string FileId;
        public long Width;
        public long Height;
        public long Duration;
        public PhotoSize Thumb;
        public string MimeType;
        public long FileSize;
    }


    public class Animation
    {
        public string FileId;
        public long Width;
        public long Height;
        public long Duration;
        public PhotoSize Thumb;
        public string FileName;
        public string MimeType;
        public long FileSize;
    }

    public class Voice
    {
        public string FileId;
        public long Duration;
        public string MimeType;
        public long FileSize;
    }

    public class VideoNote
    {
        public string FileId;
        public long Length;
        public long Duration;
        public PhotoSize Thumb;
        public long FielSize;
    }

    public class Contact
    {
        public string PhoneNumber;
        public string FirstName;
        public string LastName;
        public long UserId;
        public string Vcard;
    }

    public class Location
    {
        public float Longtitude;
        public float Latitude;
    }

    public class Venue
    {
        public Location Location;
        public string Title;
        public string Address;
        public string FoursquareId;
        public string FoursquareType;
    }

    public class Game
    {
        // not supported - empty
    }

    public class Sticker
    {
        // not supported
    }

    public class Invoice
    {
        // not supported
    }

    public class SuccessfullPayment
    {
        // not supported
    }


    public class PassportData
    {
        // not supported 
    }

    public class Message
    {
        public long MessageId;
        public User From;
        public long Date;
        public Chat Chat;
        public User ForwardFrom;
        public Chat ForwardFromChat;
        public long ForwardFromMessageId;
        public string ForwardSignature;
        public long ForwardTime;
        public Message ReplyToMessage;
        public long EditDate;
        public string MediaGroupId;
        public string AuthorSignature;
        public string Text;
        public List<MessageEntity> Entities;
        public List<MessageEntity> CaptionEntities;
        public Audio Audio;
        public Document Document;
        public Animation Animation;
        public Game Game;
        public List<PhotoSize> Photo;
        public Sticker Sticker;
        public Video Video;
        public Voice Voice;
        public VideoNote VideoNote;
        public string Caption;
        public Contact Contact;
        public Location Location;
        public Venue Venue;
        public List<User> NewChatMembers;
        public User LeftChannelMember;
        public String NewChatTitle;
        public List<PhotoSize> NewChatPhoto;
        public bool DeleteChatPhoto;
        public bool GroupChatCreated;
        public bool SupergroupChatCreated;
        public bool ChannelChatCreated;
        public long MigrateToChatId;
        public long MigrateFromChatId;
        public Message PinnedMessage;
        public Invoice Invoice;
        public SuccessfullPayment SuccessfullPayment;
        public string ConnectedWebsite;
        public PassportData PassportData;
    }

    public class InlineQuery
    {
        public string Id;
        public User From;
        public Location Location;
        public string Query;
        public string Offset;
    }

    public class ChosenInlineResult
    {
        public string ResultId;
        public User From;
        public Location Location;
        public string InlineMessageId;
        public string Query;

    }

    public class CallbackQuery
    {
        public string Id;
        public User From;
        public Message Message;
        public string InlineMessageId;
        public string ChatInstance;
        public string Data;
        public string GameShortName;

    }

    public class ShippingAddress
    {
        public string CountryCode;
        public string State;
        public string City;
        public string StreetLine1;
        public string StreetLine2;
        public string PostCode;
    }

    public class ShippingQuery
    {
        public string Id;
        public User From;
        public string InvoicePayload;
        public ShippingAddress ShippingAddress;
    }

    public class PreCheckoutQuery
    {
        // not supported
    }

    public class Update
    {
        public long UpdateId;

        public Message Message; 
        public Message EditedMessage;
        public Message ChannelPost;
        public Message EditedChannelPost;

        public InlineQuery InlineQuery;

        public ChosenInlineResult ChosenInlineResult;

        public CallbackQuery CallbackQuery;
        public ShippingQuery ShippingQuery;

        public PreCheckoutQuery PreCheckoutQuery;

        public static Update FromJsonString(string json)
        {
            Update ret = null;

            try
            {
                ret = JsonConvert.DeserializeObject<Update>(json, JsonSettings.IgnoreUnknowns);
            }
            catch (JsonSerializationException ex)
            {
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger?.Error(ex, $"Failed to parse Json: {json}");
            }
            return ret;
        }

        public string  ToJsonString()
        {
            return JsonConvert.SerializeObject(this, JsonSettings.IgnoreUnknowns);
        }
    }
}
