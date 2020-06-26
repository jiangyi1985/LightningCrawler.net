using System;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Crawler
{
    public class CrawlResult
    {
        public CrawlResult(string absoluteUri)
        {
            AbsoluteUri = absoluteUri;
        }
        public string AbsoluteUri { get; set; }

        public DateTime? FailedAt { get; set; }
        public string FailException { get; set; }

        public DateTime? CrawledAt { get; set; }
        public decimal? TimeTaken { get; set; }
        public int? StatusCode { get; set; }
        public string StatusCodeStr { get; set; }

        public string LocationAbsoluteUri { get; set; }

        //public HtmlDocument? Doc { get; set; }
        public int? ContentLength { get; set; }
        public string Content { get; set; }
        public string Canonical { get; set; }
        //public string Location { get; set; }


        public List<CrawledLink> LinkAbsoluteUris { get; set; }


        public DateTime? BrowserFailedAt { get; set; }
        public string BrowserFailedException { get; set; }
        public DateTime? BrowserCrawledAt { get; set; }
        public string BrowserContent { get; set; }
    }

    public class CrawledLink
    {
        public string AbsoluteUri { get; set; }
        public bool? IsBrowserRequired { get; set; }
    }
}