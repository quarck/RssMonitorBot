using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.Globalization;

namespace RssMonitorBot
{
    public class RssReader : IRssReader
    {
        private HttpClient _httpClient;

        public RssReader()
        {
            _httpClient = new HttpClient();
            _httpClient.MaxResponseContentBufferSize = 1024 * 256;
        }

        public async Task<RssFeed> FetchAndParse(string uri)
        {
            XmlDocument document = null; 
            try
            {
                string resp = await _httpClient.GetStringAsync(uri);

                if (!string.IsNullOrEmpty(resp))
                {
                    Console.WriteLine($"Got Xml: {resp}");
                    document = new XmlDocument();
                    document.LoadXml(resp);
                    Console.WriteLine("parse ok");
                }
                else
                {
                    Console.WriteLine("Fetch failed");
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.ToString());
            }
            catch (XmlException e)
            {
                Console.WriteLine(e.ToString());
                document = null;
            }

            return document != null ? ParseFeed(document) : null;
        }

        private RssFeed ParseFeed(XmlDocument document)
        {
            Console.WriteLine("parse feed enter");
            XmlNode root = document;

            if (!root.HasChildNodes)
                return null;

            XmlNode rss = root["rss"];
            if (rss == null || !rss.HasChildNodes)
                return null;

            XmlNode channel = rss["channel"];
            if (channel == null || !channel.HasChildNodes)
                return null;

            var ret = new RssFeed();
            ret.Items = new List<RssFeedItem>();

            for (int i = 0; i < channel.ChildNodes.Count; i++)
            {
                var child = channel.ChildNodes[i];
                switch (child.Name.ToLower())
                {
                    case "title":
                        ret.Title = child.InnerText;
                        break;
                    case "link":
                        ret.Link = child.InnerText;
                        break;
                    case "description":
                        ret.Description = child.InnerText;
                        break;
                    case "pubDate":
                        ret.PublicationDate = ParseDate(child.InnerText ?? "");
                        break;

                    case "lastBuildDate":
                        ret.LastBuildDate = ParseDate(child.InnerText ?? "");
                        break;

                    case "item":
                        {
                            var parsed = ParseItem(child);
                            if (parsed != null)
                                ret.Items.Add(parsed);
                            break;
                        }
                }
            }

            return ret;
        }

        private RssFeedItem ParseItem(XmlNode node)
        {
            if (!node.HasChildNodes)
                return null;

            RssFeedItem ret = new RssFeedItem();

            var title = node["title"];
            var description = node["description"];

            if (title == null && description == null)
                return null;

            ret.Title = title?.InnerText ?? "";
            ret.Description = description?.InnerText ?? "";
            ret.Link = node["link"]?.InnerText ?? "";
            ret.PublicationDate = ParseDate(node["pubDate"]?.InnerText ?? "");
            ret.Guid = node["guid"]?.InnerText ?? "";
            ret.EnclosureUrl = node["enclosure"]?.Attributes["url"]?.Value ?? "";

            return ret;        
        }

        private DateTime ParseDate(string dateString)
        {
            var split = dateString.Split(' ');
            if (split.Length == 6)
            {
                var dd = split[1];
                var MMM = split[2];
                var yyyy = split[3];
                var HHmmss = split[4];
                if (dd.Length == 1)
                    dd = "0" + dd;

                var zzzz = split[5];
                var zzzzLen = zzzz.Length;
                if (zzzzLen > 4)
                {
                    zzzz = zzzz.Substring(0, zzzzLen - 2) + ":" + zzzz.Substring(zzzzLen - 2);
                }

                if (DateTime.TryParseExact($"{dd} {MMM} {yyyy} {HHmmss} {zzzz}",
                    "dd MMM yyyy HH:mm:ss zzzz",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dateValue))
                {
                    return dateValue;
                }
            }

            return DateTime.MinValue;
        }


    }
}
