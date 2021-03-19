using NUnit.Framework;

namespace Crawler.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var browserWebCrawler = new BrowserWebCrawler();

            CrawlResult crawlResult = browserWebCrawler.CrawlPage(new CrawlPlan() { AbsoluteUri= "https://rally.io/" });

            Assert.Pass();
        }
    }
}