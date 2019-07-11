using NUnit.Framework;
using RssMonitorBot;
using System.Threading.Tasks;

namespace Tests
{
    public class Tests
    {
        string _badRss = @"
<abc>/wrong>
";
        RssReader _reader = null;

        [SetUp]
        public void Setup()
        {
            _reader = new RssReader();
        }

        [Test]
        public void TestWrongRss()
        {
            var doc = _reader.ParseFeedXml("test", _badRss);
            Assert.IsNull(doc);
        }

        [Test]
        public void TestGoodRss()
        {
            Task<RssFeed> task = _reader.FetchAndParse("https://adamsitnik.com/feed.xml");
            var doc = task.Result;
            Assert.IsNotNull(doc);
        }
    }
}