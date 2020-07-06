using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;

namespace Crawler
{
    public class BrowserWebCrawler
    {
        private readonly int _pageLoadWait;
        private IWebDriver driver;
        public BrowserWebCrawler(int pageLoadWait=0)
        {
            _pageLoadWait = pageLoadWait;
            var chromeOptions = new ChromeOptions()
            {
                
            };

            //chromeOptions.AddArgument("--host-resolver-rules=MAP www.google-analytics.com 127.0.0.1");
            chromeOptions.AddArgument("--host-resolver-rules=MAP www.google-analytics.com ~NOTFOUND" +
                                      ",MAP www.google.com ~NOTFOUND" +
                                      ",MAP www.googletagmanager.com ~NOTFOUND" +
                                      ",MAP i*.wp.com ~NOTFOUND" +
                                      ",MAP cdn.rawgit.com ~NOTFOUND" +
                                      ",MAP *.msecdn.net ~NOTFOUND" +
                                      ",MAP dc.services.visualstudio.com ~NOTFOUND" +
                                      ",MAP platform.twitter.com ~NOTFOUND" +
                                      ",MAP s7.addthis.com ~NOTFOUND" +
                                      ",MAP www.youtube.com ~NOTFOUND" +
                                      ",MAP secureservercdn.net ~NOTFOUND" +
                                      ",MAP tr.snapchat.com ~NOTFOUND");

            driver =new ChromeDriver(chromeOptions);
            driver.Manage().Timeouts().PageLoad=TimeSpan.FromSeconds(30);
        }

        public CrawlResult CrawlPage(CrawlPlan plan)
        {
            var crawlResult = new CrawlResult(plan.AbsoluteUri);


            string driverPageSource;

            //using (IWebDriver driver = new ChromeDriver(new ChromeOptions() { }))
            //{
            //WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            try
            {

                //todo: disable redirect
                driver.Navigate().GoToUrl(plan.AbsoluteUri);

                if (_pageLoadWait>0)
                    Thread.Sleep(TimeSpan.FromSeconds(_pageLoadWait));

                driverPageSource = driver.PageSource;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                crawlResult.BrowserFailedAt = DateTime.UtcNow;
                crawlResult.BrowserFailedException = e.ToString();
                
                //throw;

                return crawlResult;
            }



            //string html = driver.ExecuteJavaScript<string>("return document.documentElement.outerHTML;");


            //driver.FindElement(By.Name("q")).SendKeys("cheese" + Keys.Enter);
            //IWebElement firstResult = wait.Until(ExpectedConditions.ElementExists(By.TagName("h1")));
            //Console.WriteLine(firstResult.GetAttribute("textContent"));
            //}



            //crawlResult.Doc = doc;
            crawlResult.BrowserCrawledAt = DateTime.UtcNow;
            crawlResult.BrowserContent = driverPageSource;



            //find links
            var links = driver.FindElements(By.XPath("//a[@href]"));
            if (links != null)
            {
                //Console.WriteLine($"\tfound {links.Count} child links.");
                var linksToSave = new List<CrawledLink>();
                foreach (var link in links)
                {
                    var href = link.GetAttribute("href");
                    //if(href.Contains("fortnite-stats/")) Debugger.Break();

                    var decoded = HttpUtility.HtmlDecode(href);
                    //if (decoded != href)
                    //    Debugger.Break();

                    if (decoded == "" || decoded.StartsWith("javascript:") || decoded.StartsWith("mailto:") || decoded.StartsWith("skype:"))
                        continue;

                    try
                    {
                        var childUri = Util.GetUriObjectFromUriString(decoded, plan.AbsoluteUri);

                        //no duplicated links
                        if (linksToSave.All(o => o.AbsoluteUri != childUri.AbsoluteUri))
                            linksToSave.Add(new CrawledLink()
                            {
                                AbsoluteUri = childUri.AbsoluteUri,
                                IsBrowserRequired = true,
                            });
                    }
                    catch (UriFormatException e) //for mal-formated uris, just add them into the list without using System.Uri
                    {
                        if (linksToSave.All(o => o.AbsoluteUri != decoded))
                            linksToSave.Add(new CrawledLink()
                            {
                                AbsoluteUri = decoded,
                                IsBrowserRequired = true,
                            });

                        Console.WriteLine(e);
                    }
                }

                crawlResult.LinkAbsoluteUris = linksToSave;
            }

            return crawlResult;
        }

        ~BrowserWebCrawler()
        {
            Console.WriteLine($"BrowserWebCrawler{Thread.CurrentThread.ManagedThreadId} is quiting!!!");
        }
    }
}
