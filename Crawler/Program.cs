using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading;
using System.Web;
using Common.CrawlerDbContext;
using Common.Util;
//using EFCore.BulkExtensions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Uri = Common.CrawlerDbContext.Uri;

namespace Crawler
{
    class Program
    {
        //const int MAX_URI_LEN = 450;
        static void Main(string[] args)
        {
            ThreadPool.SetMinThreads(1000, 1000);

            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();


            //var db = CrawlerContext.Create(Configuration.GetConnectionString("CrawlerDatabase"));
            //var u1 = new Uri { AbsoluteUri = "key1" };
            //var u2 = new Uri { AbsoluteUri = "key2" };
            //var u3 = new Uri { AbsoluteUri = "key1" };
            //var list = new List<Uri> { u1, u2, u3 };

            ////db.Uri.Add(u1);
            ////db.Uri.Add(u2);
            ////db.SaveChanges();
            ////db.BulkInsertOrUpdate(list);

            //db.BulkMerge(list, options => options.ColumnPrimaryKeyExpression = o => o.AbsoluteUri);



            const string startPage = "https://www.domain.com/";
            //var browserWebCrawler = new BrowserWebCrawler();
            //var crawlResult = browserWebCrawler.CrawlPage(new CrawlPlan() {AbsoluteUri = "https://www.playerauctions.com/wow-account"});


            //var staticWebCrawler = new StaticWebCrawler();
            //var crawlResult = staticWebCrawler.CrawlPage(new CrawlPlan() { AbsoluteUri = "https://www.playerauctions.com/wow-gold/" });



            var crawler = new LightningCrawler(Configuration.GetConnectionString("CrawlerDatabase"), startPage,new string[]
            {
                "www.domain.com",
                "subdomain1.domain.com",
                "subdomain2.domain.com",
            },
                20,
                2,
                0);

            crawler.Run();
        }

        ~Program()
        {

        }
    }
}
