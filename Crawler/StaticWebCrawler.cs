using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Common.CrawlerDbContext;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;

namespace Crawler
{
    public class StaticWebCrawler
    {
        private string _redirectLocation = null;

        public CrawlResult CrawlPage(CrawlPlan plan)
        {
            var crawlResult = new CrawlResult(plan.AbsoluteUri);

            var web = new HtmlWeb();
            web.CaptureRedirect = true;
            web.PreRequest= request => WebPreRequest(request);
            web.PostResponse = (request, response) => HtmlWeb_PostResponse(request, response);

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            //Console.WriteLine($"loading...\t{plan.AbsoluteUri}");
            HtmlDocument doc;
            try
            {
                doc = web.Load(plan.AbsoluteUri);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //if (e is HtmlWebException && e.Message.StartsWith("Unsupported uri scheme:"))
                //{
                //}
                //else
                //{
                    crawlResult.FailedAt = DateTime.UtcNow;
                    crawlResult.FailException = e.ToString();
                //}

                return crawlResult;
            }

            stopWatch.Stop();

            var statusCode = (int) web.StatusCode;
            var statusCodeString = web.StatusCode.ToString();
            var timeTaken = stopWatch.Elapsed.TotalSeconds;

            crawlResult.CrawledAt = DateTime.UtcNow;
            crawlResult.StatusCodeStr = statusCodeString;
            crawlResult.StatusCode = statusCode;
            crawlResult.TimeTaken = (decimal?) timeTaken;

            //not 200 OK
            if (web.StatusCode != HttpStatusCode.OK)
            {
                //Console.WriteLine($"\tstatus code = {statusCode}");

                //3xx redirect
                if (statusCode / 100 == 3)
                {
                    try
                    {
                        //Console.WriteLine($"\tfound redirect {statusCode} {_redirectLocation}");
                        var locationUri = Util.GetUriObjectFromUriString(_redirectLocation, plan.AbsoluteUri);
                        //Console.WriteLine($"\tfound new location {locationUri.AbsoluteUri}");

                        crawlResult.LocationAbsoluteUri = locationUri.AbsoluteUri;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }

                return crawlResult;
            }

            //not a document
            if (doc.ParsedText == null)
            {
                return crawlResult;
            }

            //crawlResult.Doc = doc;
            crawlResult.ContentLength = doc.ParsedText.Length;
            crawlResult.Content = doc.ParsedText;

            //find links
            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                //Console.WriteLine($"\tfound {links.Count} child links.");
                var linksToSave = new List<CrawledLink>();
                foreach (var link in links)
                {
                    var href = link.Attributes["href"].Value;
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
                            });
                    }
                    catch (UriFormatException e)//for mal-formated uris, just add them into the list without using System.Uri
                    {
                        if (linksToSave.All(o => o.AbsoluteUri != decoded))
                            linksToSave.Add(new CrawledLink()
                            {
                                AbsoluteUri = decoded,
                            });

                        Console.WriteLine(e);
                    }
                }

                crawlResult.LinkAbsoluteUris = linksToSave;
            }

            //find canonical
            var canonicalLinks = doc.DocumentNode.SelectNodes("//link[@rel='canonical']");
            if (canonicalLinks != null && canonicalLinks.Count > 0)
            {
                var canonicalLinkValue = canonicalLinks[0].Attributes["href"].Value;
                //Console.WriteLine($"\tfound canonical");
                crawlResult.Canonical = canonicalLinkValue;
            }

            return crawlResult;
        }

        private bool WebPreRequest(HttpWebRequest request)
        {
            //request.AllowAutoRedirect = false;
            //request.MaximumAutomaticRedirections = 1;

            //request.Headers.Add("Accept-Language", "en-US,en;q=0.9,zh-CN;q=0.8,zh;q=0.7");

            return true;
        }

        private void HtmlWeb_PostResponse(HttpWebRequest request, HttpWebResponse response)
        {
            var statusCode = (int)response.StatusCode;
            if (statusCode / 100 == 3)
                _redirectLocation = response.Headers["Location"];
            else
                _redirectLocation = null;
        }
    }
}
