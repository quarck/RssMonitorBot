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
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private HttpClient _httpClient;

        public RssReader()
        {
            _httpClient = new HttpClient();
            _httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 8;
        }

        public async Task<RssFeed> FetchAndParse(string uri)
        {
            var uriLc = uri.ToLower();
            if (!uriLc.StartsWith("http://") && !uriLc.StartsWith("https://"))
            {
                uri = "https://" + uri;
            }

            XmlDocument document = null; 
            try
            {
                string resp = await _httpClient.GetStringAsync(uri);

                if (!string.IsNullOrEmpty(resp))
                {
                    logger.Info($"Received XML document of {resp.Length} bytes length for uri {uri}");
                    document = new XmlDocument();
                    document.LoadXml(resp);
                    logger.Info($"Received XML document of {resp.Length} bytes length for uri {uri}, and parsed OK");
                }
                else
                {
                    logger.Error($"Fetch failed for {uri}: received an empty document");
                }
            }
            catch (HttpRequestException ex)
            {
                logger.Error(ex, $"Fetch failed for {uri}");
                document = null;
            }
            catch (XmlException ex)
            {
                logger.Error(ex, $"Fetch failed for {uri}");
                document = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Fetch failed for {uri}");
                document = null;
            }

            string origin = "RSS";
            if (Uri.TryCreate(uri, UriKind.Absolute, out var uriObj))
            {
                origin = uriObj.Host;
            }

            return document != null ? ParseFeed(origin, document) : null;
        }

        public RssFeed ParseFeedXml(string origin, string documentContent)
        {
            XmlDocument document = null;
            try
            {
                document = new XmlDocument();
                document.LoadXml(documentContent);
            }
            catch (XmlException ex)
            {
                logger.Error(ex, $"Rss Parsing failed");
                document = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Rss Parsing failed");
                document = null;
            }

            return document != null ? ParseFeed(origin, document) : null;
        }

        private RssFeed ParseRssFeed(string origin, XmlDocument document, XmlNode rss)
        {
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
                    case "pubdate":
                        ret.PublicationDate = ParseRssDate(child.InnerText ?? "");
                        break;

                    case "lastbuilddate":
                        ret.LastBuildDate = ParseRssDate(child.InnerText ?? "");
                        break;

                    case "item":
                        {
                            var parsed = ParseRssItem(origin, child);
                            if (parsed != null)
                                ret.Items.Add(parsed);
                            break;
                        }
                }
            }

            return ret;
        }

        private RssFeed ParseAtomFeed(string origin, XmlDocument document, XmlNode feed)
        {
            var ret = new RssFeed();
            ret.Items = new List<RssFeedItem>();

            for (int i = 0; i < feed.ChildNodes.Count; i++)
            {
                var child = feed.ChildNodes[i];
                switch (child.Name.ToLower())
                {
                    case "title":
                        ret.Title = child.InnerText;
                        break;
                    case "link":
                        ret.Link = child.InnerText;
                        break;
                    case "subtitle":
                        ret.Description = child.InnerText;
                        break;
                    case "updated":
                        ret.LastBuildDate = ret.PublicationDate = ParseISO8601String(child.InnerText ?? "");
                        break;

                    case "entry":
                        {
                            var parsed = ParseAtomEntry(origin, child);
                            if (parsed != null)
                                ret.Items.Add(parsed);
                            break;
                        }
                }
            }

            return ret;
        }

        private RssFeed ParseFeed(string origin, XmlDocument document)
        {
            XmlNode root = document;

            if (!root.HasChildNodes)
                return null;

            // Try parsing as RSS
            XmlNode rss = root["rss"];
            if (rss != null && rss.HasChildNodes)
                return ParseRssFeed(origin, document, rss);

            // Try parsing as Atom
            XmlNode feed = root["feed"];
            if (feed != null && feed.HasChildNodes)
                return ParseAtomFeed(origin, document, feed);

            // None matched
            return null;
        }

        private RssFeedItem ParseRssItem(string origin, XmlNode node)
        {
            if (!node.HasChildNodes)
                return null;

            RssFeedItem ret = new RssFeedItem();

            var title = node["title"];
            var description = node["description"];

            if (title == null && description == null)
                return null;

            ret.Origin = origin;
            ret.Title = title?.InnerText ?? "";
            ret.Description = description?.InnerText ?? "";
            ret.Link = node["link"]?.InnerText ?? "";
            ret.PublicationDate = ParseRssDate(node["pubDate"]?.InnerText ?? "");
            ret.Guid = node["guid"]?.InnerText ?? "";
            ret.EnclosureUrl = node["enclosure"]?.Attributes["url"]?.Value ?? "";

            return ret;        
        }

        private RssFeedItem ParseAtomEntry(string origin, XmlNode node)
        {
            if (!node.HasChildNodes)
                return null;

            RssFeedItem ret = new RssFeedItem();

            var title = node["title"];
            var content = node["content"];

            if (title == null && content == null)
                return null;

            ret.Origin = origin;
            ret.Title = title?.InnerText ?? "";
            ret.Description = content?.InnerText ?? "";
            ret.Link = node["link"]?.GetAttribute("href") ?? "";
            ret.PublicationDate = ParseISO8601String(node["published"]?.InnerText ?? "");
            ret.Guid = node["id"]?.InnerText ?? "";
            ret.EnclosureUrl = ret.Link;

            return ret;
        }

        private DateTime ParseRssDate(string dateString)
        {
            var split = dateString.Split(' ');
            if (split.Length == 6)
            {
                var day = split[1];
                var month = split[2];
                var year = split[3];
                var time = split[4];
                if (day.Length == 1)
                    day = "0" + day;

                var tz = split[5];
                var tzLen = tz.Length;
                if (tzLen > 4)
                {
                    tz = tz.Substring(0, tzLen - 2) + ":" + tz.Substring(tzLen - 2);
                }

                if (DateTime.TryParseExact($"{day} {month} {year} {time} {tz}",
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


        static readonly string[] iso8601DateFormats = { 
            // Basic formats
            "yyyyMMddTHHmmsszzz",
            "yyyyMMddTHHmmsszz",
            "yyyyMMddTHHmmssZ",
            // Extended formats
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:sszz",
            "yyyy-MM-ddTHH:mm:ssZ",
            // All of the above with reduced accuracy
            "yyyyMMddTHHmmzzz",
            "yyyyMMddTHHmmzz",
            "yyyyMMddTHHmmZ",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mmzz",
            "yyyy-MM-ddTHH:mmZ",
            // Accuracy reduced to hours
            "yyyyMMddTHHzzz",
            "yyyyMMddTHHzz",
            "yyyyMMddTHHZ",
            "yyyy-MM-ddTHHzzz",
            "yyyy-MM-ddTHHzz",
            "yyyy-MM-ddTHHZ"
            };

        private static DateTime ParseISO8601String(string str)
        {
            return DateTime.ParseExact(str, iso8601DateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
    }
}
