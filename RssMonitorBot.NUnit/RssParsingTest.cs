using NUnit.Framework;
using RssMonitorBot;

namespace Tests
{
    public class Tests
    {
        string _badRss = "<abc>/wrong>";
        RssReader _reader = null;

        [SetUp]
        public void Setup()
        {
            _reader = new RssReader();
        }

        [Test]
        public void TestWrongRss()
        {
            var doc = _reader.ParseFeedXml(_badRss);
            Assert.IsNull(doc);
        }
    }
}