using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RssMonitorBot
{
    public class RssFeedItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime PublicationDate { get; set; }
        public string Guid { get; set; }
        public string EnclosureUrl { get; set; } // picture 

        public override string ToString()
        {
            return Title + "\n" + Link + "\n" + Description + "\n" + EnclosureUrl + "\n" + PublicationDate + "\n"; 
        }
    }

    public class RssFeed
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime PublicationDate { get; set; }
        public DateTime LastBuildDate { get; set; }

        public List<RssFeedItem> Items;

        public override string ToString()
        {
            return string.Join("\n\n", Items.Select(x => x.ToString()));
        }
    }
}
