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

        [Test]
        public void TestMorningBrewAtom()
        {
            Task<RssFeed> task = _reader.FetchAndParse("http://feeds.feedburner.com/ReflectivePerspective");
            var doc = task.Result;
            Assert.IsNotNull(doc);
        }

        [Test]
        public void TestIsoCpp()
        {
            // 
        }

        [Test]
        public void TestMetIe()
        { 
            Task<RssFeed> task = _reader.FetchAndParse("https://www.met.ie/warningsxml/rss.xml");
            var doc = task.Result;
            Assert.IsNotNull(doc);

        }
    }
}