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
            _httpClient.MaxResponseContentBufferSize = 1024 * 256;
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

            return document != null ? ParseFeed(document) : null;
        }

        public RssFeed ParseFeedXml(string documentContent)
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

            return document != null ? ParseFeed(document) : null;
        }

        private RssFeed ParseFeed(XmlDocument document)
        {
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
                    case "pubdate":
                        ret.PublicationDate = ParseDate(child.InnerText ?? "");
                        break;

                    case "lastbuilddate":
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
    }
}
