using System;
using System.Collections.Generic;
using System.Text;

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
    }

    [Serializable]
    public class UserFeedPubDates
    {
        public Dictionary<string, DateTime> PubDates; // feed URI as a key
    }
}
