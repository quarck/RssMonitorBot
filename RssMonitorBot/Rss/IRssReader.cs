using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RssMonitorBot
{
    public interface IRssReader
    {
        Task<RssFeed> FetchAndParse(string uri);
    }
}
