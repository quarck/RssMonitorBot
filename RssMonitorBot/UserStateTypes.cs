using System;
using System.Collections.Generic;
using System.Text;

namespace RssMonitorBot
{
    [Serializable]
    public class AuthValidFlag
    {
        public bool AuthValid = true;
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
}
