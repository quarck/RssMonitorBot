using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Linq;

namespace RssMonitorBot
{
    [Serializable]
    public class UserDetails
    {
        public bool AuthValid = true;
        public long UserId;
        public long ChatId;
    }

    [Serializable]
    public class RssUrlEntry
    {
        public string Url;
        public string[] Keywords;
    }

    [Serializable]
    public class UserRssSubscriptions
    { 
        public List<RssUrlEntry> RssEntries; 
    }

    [Serializable]
    public class UserMuteState
    {
        public bool Muted;
        public bool Stopped;

        public int DaySecondsFrom = 0;
        public int DaySecondsTo = 0;

        public void SetHours(int from, int to)
        {
            DaySecondsFrom = from * 3600;
            DaySecondsTo = to * 3600;
        }

        public bool IsMutedNow(ref DateTime dateTime)
        {
            if (Muted)
                return true;
            var daySeconds = dateTime.Hour * 3600 + dateTime.Minute * 60 + dateTime.Second;
            return (DaySecondsFrom != 0 || DaySecondsTo != 0) &&
                (daySeconds < DaySecondsFrom || daySeconds > DaySecondsTo);
        }
    }


    [Serializable]
    public class RecentNotificationEntry
    {
        public string Date;
        public string Title;
        public string Content;

        public RecentNotificationEntry(string d, string t, string c)
        {
            Date = d;
            Title = t;
            Content = c;
        }

        public RecentNotificationEntry()
        {
            Date = ""; Title = ""; Content = "";
        }
    }

    [Serializable]
    public class UserFeedPubDates
    {
        [NonSerialized]
        private static int MAX_RECENTS = 10000;

        public SerializableDictionary<string, DateTime> PubDates; // feed URI as a key
        public List<string> RecentLinks;
        public List<RecentNotificationEntry> RecentNotifications;

        public void AddRecentLink(string url)
        {
            if (RecentLinks == null)
            {
                RecentLinks = new List<string>();
            }

            if (RecentLinks.Count == MAX_RECENTS)
            {
                RecentLinks.RemoveAt(0);
            }

            RecentLinks.Add(url);
        }

        private string todayDateAsString()
        {
            return DateTime.Today.ToString("ddMMyyyy");
        }

        public void AddRecentNotification(string title, string content)
        {
            if (RecentNotifications == null)
            {
                RecentNotifications = new List<RecentNotificationEntry>();
            }

            if (RecentNotifications.Count == MAX_RECENTS)
            {
                RecentNotifications.RemoveAt(0);
            }

            RecentNotifications.Add(new RecentNotificationEntry(todayDateAsString(), title, content));
        }

        public bool IsRecent(string url)
        {
            if (RecentLinks == null)
                return false;
            return RecentLinks.Contains(url);
        }

        public bool IsRecentNotification(string title, string content)
        {
            if (RecentNotifications == null)
                return false;
            var today = todayDateAsString();
            return RecentNotifications.Any(x => x.Date == today && x.Title == title && x.Content == content);
        }
    }
}
