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
using Nest;
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


            if (args.Length > 0 && args[0] == "indexer")
            {
                var settings = new ConnectionSettings(new System.Uri("http://localhost:9200"))
                    .DefaultIndex("uri").BasicAuthentication("elastic", "");
                var elasticClient = new ElasticClient(settings);
                //var uri = new Uri { AbsoluteUri = "key1", BrowserContent = "<fkjaslkdf>a sdlfjlasjdflsM</asdf> lkafsjiw fasd fjl<a></a>", CrawledAt = DateTime.UtcNow };
                //var indexResponse = elasticClient.IndexDocument(uri);
                var db = CrawlerContext.Create(Configuration.GetConnectionString("CrawlerDatabase"));
                var uriDocuments = db.Uri.Where(o => o.BrowserContent != null)
                    .OrderBy(o => o.Id)
                    .Select(o => new UriDocument()
                    {
                        AbsoluteUri = o.AbsoluteUri,
                        BrowserHtml = o.BrowserContent,
                        Id = o.Id,
                        OriginalUriString = o.OriginalString,
                    })
                    .ToList();
                var htmlDoc = new HtmlDocument();
                foreach (var doc in uriDocuments)
                {
                    htmlDoc.LoadHtml(doc.BrowserHtml);
                    var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//html");
                    doc.BrowserText= htmlBody.InnerText.Trim();
                }
                var bulkAllObservable = elasticClient.BulkAll(uriDocuments, b => b
                        .Index("uri")
                        // how long to wait between retries
                        .BackOffTime("30s")
                        // how many retries are attempted if a failure occurs
                        .BackOffRetries(2)
                        // refresh the index once the bulk operation completes
                        .RefreshOnCompleted()
                        // how many concurrent bulk requests to make
                        .MaxDegreeOfParallelism(Environment.ProcessorCount)
                        // number of items per bulk request
                        .Size(1000)
                        //.RetryDocumentPredicate((item, person) =>
                        //{
                        //    // decide if a document should be retried in the event of a failure
                        //    return item.Error.Index == "even-index" && person.FirstName == "Martijn";
                        //})
                        .DroppedDocumentCallback((item, uri) =>
                        {
                            // if a document cannot be indexed this delegate is called
                            Console.WriteLine($"Unable to index: {item} {uri}");
                        })
                    )
                    // Perform the indexing, waiting up to 15 minutes. 
                    // Whilst the BulkAll calls are asynchronous this is a blocking operation
                    .Wait(TimeSpan.FromMinutes(15), next =>
                    {
                        // do something on each response e.g. write number of batches indexed to console
                        Console.WriteLine($"ES Bulked Items: {next.Items.Count}");
                        Console.WriteLine(
                            $"{next.Items.GroupBy(o => o.Result).Select(o => o.Key + ":" + o.Count()).Aggregate((o, n) => o + " " + n)}");
                    });
                //Console.ReadKey();
                return;
            }



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
