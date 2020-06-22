using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Common.CrawlerDbContext;
using HtmlAgilityPack;

namespace Crawler
{
    public class WebCrawler
    {
        private string _redirectLocation = null;

        public CrawlResult CrawlPage(CrawlPlan plan)
        {
            var crawlResult = new CrawlResult(plan.AbsoluteUri);

            var web = new HtmlWeb();
            web.CaptureRedirect = true;
            //web.PreRequest= request => WebPreRequest(request);
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

            //find links
            var links = doc.DocumentNode.SelectNodes("//a[@href]");
            if (links != null)
            {
                //Console.WriteLine($"\tfound {links.Count} child links.");
                var linksToSave = new List<string>();
                foreach (var link in links)
                {
                    var href = link.Attributes["href"].Value;
                    //if(href.Contains("fortnite-stats/")) Debugger.Break();
                    if (href == "" || href.StartsWith("javascript:") || href.StartsWith("mailto:"))
                        continue;

                    var decoded = HttpUtility.HtmlDecode(href);

                    try
                    {
                        var childUri = Util.GetUriObjectFromUriString(decoded, plan.AbsoluteUri);

                        //no duplicated links
                        if (linksToSave.All(o => o != childUri.AbsoluteUri))
                            linksToSave.Add(childUri.AbsoluteUri);
                    }
                    catch (UriFormatException e)
                    {
                        if (linksToSave.All(o => o != decoded))
                            linksToSave.Add(decoded);

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
            request.AllowAutoRedirect = false;
            request.MaximumAutomaticRedirections = 1;
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
