using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Common.CrawlerDbContext;
using Uri = Common.CrawlerDbContext.Uri;

namespace Crawler
{
    public class LightningCrawler
    {
        private readonly string _dbConStr;
        private readonly int _crawlerThreadCount;
        private readonly int _browserCrawlerThreadCount;
        private readonly CrawlerContext _db;
        private readonly System.Uri _startPageUri;
        private readonly string[] _hosts;

        private readonly ConcurrentQueue<CrawlPlan> _queuePlan = new ConcurrentQueue<CrawlPlan>();
        private readonly ConcurrentQueue<CrawlResult> _queueCrawlResult = new ConcurrentQueue<CrawlResult>();
        private readonly ConcurrentQueue<CrawlPlan> _queueBrowserPlan = new ConcurrentQueue<CrawlPlan>();
        private readonly Dictionary<string,int> _dicUriIdMapping = new Dictionary<string, int>();

        private readonly Dictionary<string, CrawlStatus> _dicPlanned = new Dictionary<string, CrawlStatus>();
        private readonly Dictionary<string, CrawlStatus> _dicPlannedBrowser = new Dictionary<string, CrawlStatus>();
        //private readonly Dictionary<int, string> _dicUriPages = new Dictionary<int, string>();

        public LightningCrawler(string dbConStr, string startPage, string[] hosts = null, int crawlerThreadCount=20, int browserCrawlerThreadCount=5)
        {
            _dbConStr = dbConStr;
            _crawlerThreadCount = crawlerThreadCount;
            _browserCrawlerThreadCount = browserCrawlerThreadCount;
            _db = CrawlerContext.Create(dbConStr);
            _startPageUri = new System.Uri(startPage);
            _hosts = hosts ?? new string[] { _startPageUri.Host };
        }

        public void Run()
        {
            var startPage = _db.Uri.FirstOrDefault(o => o.AbsoluteUri == _startPageUri.AbsoluteUri);
            if (startPage == null)
            {
                Console.WriteLine("Adding START_PAGE to db...");
                startPage = NewUriDbModel(_startPageUri);
                _db.Uri.Add(startPage);
                _db.SaveChanges();
            }

            Task.Run(PlannerStatic);

            for (int i = 0; i < _crawlerThreadCount; i++)
            {
                Task.Run(CrawlerStatic);
            }

            Task.Run(Storer);


            Task.Run(PlannerBrowser);
            for (int i = 0; i < _browserCrawlerThreadCount; i++)
            {
                Task.Run(CrawlerBrowserWebDriver);
            }

            while (true)
            {
                //Console.WriteLine("----------------------------Start Crawling--------------------------");
                //CrawlPages(_db, startPageHost);
                //Console.WriteLine("---------------------------- End  Crawling--------------------------");
                //Console.WriteLine();
                //Console.WriteLine();
                //Console.WriteLine();
                //Console.WriteLine();
                Console.WriteLine($"Pending Crawl: {_queuePlan.Count}\tPending Browser: {_queueBrowserPlan.Count}\tPending Store: {_queueCrawlResult.Count}");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void PlannerStatic()
        {
            while (true)
            {
                try
                {
                    if (_queuePlan.IsEmpty)
                    {
                        Console.WriteLine($"\tPlan\tplan queue is empty. checking db for new plan...");
                        var pagesToCrawl = GetPagesToCrawl(_hosts);
                        Console.WriteLine($"\tPlan\tfound {pagesToCrawl.Count} uncrawled pages. adding to queue");
                        foreach (var crawlPlan in pagesToCrawl)
                        {
                            if (!_dicPlanned.ContainsKey(crawlPlan.AbsoluteUri))
                            {
                                _queuePlan.Enqueue(crawlPlan);
                                _dicPlanned.TryAdd(crawlPlan.AbsoluteUri, null);
                            }
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"\tPlan\tplan queue has {_queuePlan.Count} items");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
        private void PlannerBrowser()
        {
            while (true)
            {
                try
                {
                    if (_queueBrowserPlan.IsEmpty)
                    {
                        Console.WriteLine($"\tPlan_Browser\tqueue is empty. checking db for new plan...");
                        var pagesToCrawl = GetPagesToBrowserCrawl(_hosts);
                        Console.WriteLine($"\tPlan_Browser\tfound {pagesToCrawl.Count} uncrawled pages. adding to queue");
                        foreach (var crawlPlan in pagesToCrawl)
                        {
                            if (!_dicPlannedBrowser.ContainsKey(crawlPlan.AbsoluteUri))
                            {
                                _queueBrowserPlan.Enqueue(crawlPlan);
                                _dicPlannedBrowser.TryAdd(crawlPlan.AbsoluteUri, null);
                            }
                        }
                    }
                    else
                    {
                        //Console.WriteLine($"\tPlan\tplan queue has {_queuePlan.Count} items");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private void CrawlerStatic()
        {
            var webCrawler = new StaticWebCrawler();
            while (true)
            {
                if (!_queuePlan.IsEmpty)
                {
                    //var pendingCrawlCount = _queuePlan.Count;
                    var pendingStoreCount = _queueCrawlResult.Count;

                    if (pendingStoreCount > 500)
                        Console.WriteLine($"\tCrawler{Thread.CurrentThread.ManagedThreadId}\ttoo many pending store {pendingStoreCount}, waiting...");
                    else
                    {
                        //Console.WriteLine($"\tCrawl\tstart crawling for next items (20 at max) ");
                        for (int i = 0; i < 100; i++)
                        {
                            var tryDequeue = _queuePlan.TryDequeue(out var plan);

                            if (!tryDequeue) break;

                            Console.WriteLine($"\t\t\tCrawler{Thread.CurrentThread.ManagedThreadId}\t{plan.AbsoluteUri}");
                            var crawlResult = webCrawler.CrawlPage(plan);

                            //Console.WriteLine($"\tCrawl\tAdding crawl result to store queue...\t{plan.AbsoluteUri}");
                            _queueCrawlResult.Enqueue(crawlResult);

                            ////if crawl fails, remove from dicPlan so that it can be added again
                            //if (crawlResult.FailException != null)
                            //    _dicPlanned.Remove(crawlResult.AbsoluteUri);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine($"\tCrawl\tno crawling required");
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
        private void CrawlerBrowserWebDriver()
        {
            var browserCrawler = new BrowserWebCrawler();
            while (true)
            {
                if (!_queueBrowserPlan.IsEmpty)
                {
                    //var pendingCrawlCount = _queuePlan.Count;
                    var pendingStoreCount = _queueCrawlResult.Count;

                    if (pendingStoreCount > 500)
                        Console.WriteLine($"\tCrawler_Browser{Thread.CurrentThread.ManagedThreadId}\ttoo many pending store {pendingStoreCount}, waiting...");
                    else
                    {
                        //Console.WriteLine($"\tCrawl\tstart crawling for next items (20 at max) ");
                        for (int i = 0; i < 10; i++)
                        {
                            var tryDequeue = _queueBrowserPlan.TryDequeue(out var plan);

                            if (!tryDequeue) break;

                            Console.WriteLine($"\t\t\t\tBrowser{Thread.CurrentThread.ManagedThreadId}\t{plan.AbsoluteUri}");
                            var crawlResult = browserCrawler.CrawlPage(plan);

                            //Console.WriteLine($"\tCrawl\tAdding crawl result to store queue...\t{plan.AbsoluteUri}");
                            _queueCrawlResult.Enqueue(crawlResult);

                            ////if crawl fails, remove from dicPlan so that it can be added again
                            //if (crawlResult.BrowserFailedException != null)
                            //    _dicPlannedBrowser.Remove(crawlResult.AbsoluteUri);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine($"\tCrawl\tno crawling required");
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void Storer()
        {
            //get all uri id into memory
            var uriIds=_db.Uri.Select(o => new {o.AbsoluteUri, o.Id}).ToList();
            foreach (var uriId in uriIds)
            {
                _dicUriIdMapping.Add(uriId.AbsoluteUri,uriId.Id);
            }

            //start storing
            while (true)
            {
                var pendingStoreCount = _queueCrawlResult.Count;
                if (pendingStoreCount > 0)
                {
                    var list = new List<CrawlResult>();
                    for (int i = 0; i < pendingStoreCount; i++)
                    {
                        var tryDequeue = _queueCrawlResult.TryDequeue(out var result);
                        if (!tryDequeue) break;
                        list.Add(result);
                    }

                    SaveCrawlResults(list);
                }
                else
                {
                    //Console.WriteLine($"\tStore\tcrawl result queue empty");
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void SaveCrawlResults(List<CrawlResult> list)
        {
            //--------------------------------save pages--------------------------------------

            if (list.Count!=list.Select(o=>o.AbsoluteUri).Distinct().Count())
                Debugger.Break();

            //crawled pages
            var pages = list.Select(o =>
            {
                var uri = NewUriDbModel(new System.Uri(o.AbsoluteUri));

                uri.FailedAt = o.FailedAt;
                uri.FailedException = o.FailException;

                uri.StatusCode = o.StatusCode;
                uri.StatusCodeString = o.StatusCodeStr;
                uri.TimeTaken = o.TimeTaken;
                uri.CrawledAt = o.CrawledAt;

                uri.ContentLength = o.ContentLength;
                uri.Content = o.Content;
                uri.Canonical = o.Canonical;

                uri.BrowserCrawledAt = o.BrowserCrawledAt;
                uri.BrowserContent = o.BrowserContent;
                uri.BrowserFailedAt = o.BrowserFailedAt;
                uri.BrowserFailedException = o.BrowserFailedException;
                //todo:db new field here

                return uri;
            }).ToList();

            //their links and redirects
            foreach (var crawlResult in list)
            {
                if (crawlResult.LocationAbsoluteUri != null)
                {
                    if (pages.All(p => p.AbsoluteUri != crawlResult.LocationAbsoluteUri))
                        pages.Add(NewUriDbModel(new System.Uri(crawlResult.LocationAbsoluteUri)));
                }

                if (crawlResult.LinkAbsoluteUris != null)
                {
                    foreach (var crawledLink in crawlResult.LinkAbsoluteUris)
                    {
                        if (pages.All(p => p.AbsoluteUri != crawledLink.AbsoluteUri))
                        {
                            try
                            {
                                var uri = new System.Uri(crawledLink.AbsoluteUri);

                                pages.Add(NewUriDbModel(uri));
                            }
                            catch (UriFormatException e)
                            {
                                pages.Add(NewUriDbModel(crawledLink.AbsoluteUri));
                            }
                        }
                    }
                }
            }

            if (pages.Count != pages.Select(o => o.AbsoluteUri).Distinct().Count())
                Debugger.Break();

            //save
            _db.BulkMerge(pages, options =>
            {
                options.ColumnPrimaryKeyExpression = o => o.AbsoluteUri;

                options.IgnoreOnMergeUpdateExpression = o => new
                {
                    o.AbsoluteUri,
                    o.AbsolutePath,
                    o.Host,
                    o.Scheme,
                    o.Fragment,
                    o.Query,
                    o.CreateAt,
                    o.OriginalString,
                };

                //The CoalesceOnMergeUpdateExpression allows you to not update any column if the specified value is null and its database
                //value is not null when BulkMerge method is executed.
                options.CoalesceOnMergeUpdateExpression = o => new
                {
                    o.FailedAt,
                    o.FailedException,
                    o.StatusCode,
                    o.StatusCodeString,
                    o.TimeTaken,
                    o.CrawledAt,
                    o.ContentLength,
                    o.Content,
                    o.Canonical,
                    o.BrowserCrawledAt,
                    o.BrowserContent,
                    o.BrowserFailedAt,
                    o.BrowserFailedException,
                    //todo:db new field here
                };
            });


            foreach (var crawlResult in list)
            {
                //if crawl fails, remove from dicPlan so that it can be added again
                if (crawlResult.FailException != null)
                    _dicPlanned.Remove(crawlResult.AbsoluteUri);

                //if crawl fails, remove from dicPlan so that it can be added again
                if (crawlResult.BrowserFailedException != null)
                    _dicPlannedBrowser.Remove(crawlResult.AbsoluteUri);
            }


            //update uri ids in memory
            foreach (var page in pages)
            {
                if (!_dicUriIdMapping.ContainsKey(page.AbsoluteUri))
                    _dicUriIdMapping.Add(page.AbsoluteUri, page.Id);
            }


            //--------------------------------save relations--------------------------------------
            var relations = new List<Relation>();
            var redirectRelations = new List<RedirectRelation>();

            foreach (var crawlResult in list)
            {
                if (crawlResult.LocationAbsoluteUri != null)
                    redirectRelations.Add(new RedirectRelation()
                    {
                        SourceId = _dicUriIdMapping[crawlResult.AbsoluteUri],
                        DestinationId = _dicUriIdMapping[crawlResult.LocationAbsoluteUri],
                        CreatedAt = DateTime.UtcNow,
                    });
                else if (crawlResult.LinkAbsoluteUris != null)
                {
                    foreach (var crawledLink in crawlResult.LinkAbsoluteUris)
                    {
                        relations.Add(new Relation()
                        {
                            ParentId = _dicUriIdMapping[crawlResult.AbsoluteUri],
                            ChildId = _dicUriIdMapping[crawledLink.AbsoluteUri],
                            CreatedAt = DateTime.UtcNow,
                            IsBrowserRequired = crawledLink.IsBrowserRequired,
                        });
                    }
                }
            }

            if (relations.Count > 0)
                _db.BulkMerge(relations, options =>
                {
                    options.IgnoreOnMergeUpdateExpression = o => new
                    {
                        o.CreatedAt,
                        o.IsBrowserRequired,//if a static relation already exists, ignore browser relations
                    };
                    //options.CoalesceOnMergeUpdateExpression = o => o.IsBrowserRequired;
                });
            if (redirectRelations.Count > 0)
                _db.BulkMerge(redirectRelations, options =>
                {
                    options.IgnoreOnMergeUpdateExpression = o => o.CreatedAt;
                });

            //Console.WriteLine($"Saved {list.Count} pages");
        }

        //private void SaveCrawlResult(CrawlResult crawlResult)
        //{
        //    var stopWatch=new Stopwatch();
        //    stopWatch.Start();

        //    var page = _db.Uri.FirstOrDefault(o => o.AbsoluteUri == crawlResult.AbsoluteUri);

        //    if (page == null) Debugger.Break();

        //    if (page.FailedAt.HasValue)
        //    {
        //        page.FailedAt = crawlResult.FailedAt;
        //        page.FailedException = crawlResult.FailException;
        //        _db.SaveChanges();
        //        return;
        //    }

        //    if (crawlResult.StatusCode != 200)
        //    {
        //        if (crawlResult.LocationAbsoluteUri != null)
        //        {
        //            var destinationPage = _db.Uri.FirstOrDefault(o => o.AbsoluteUri == crawlResult.LocationAbsoluteUri);
        //            if (destinationPage == null)
        //            {
        //                //Console.WriteLine($"\tadding destination page to db...");
        //                destinationPage = NewUriDbModel(new System.Uri(crawlResult.LocationAbsoluteUri));
        //                _db.Uri.Add(destinationPage);
        //                _db.SaveChanges();
        //            }
        //            else
        //            {
        //                //Console.WriteLine($"\tdestination page already exists in db...");
        //            }

        //            //add redirect relations
        //            var redirectRelation = _db.RedirectRelation.FirstOrDefault(o => o.SourceId == page.Id && o.DestinationId == destinationPage.Id);
        //            if (redirectRelation == null)
        //            {
        //                //Console.WriteLine($"\tsaving redirect relation to db...");
        //                redirectRelation = new RedirectRelation()
        //                {
        //                    SourceId = page.Id,
        //                    DestinationId = destinationPage.Id,
        //                    CreatedAt = DateTime.UtcNow,
        //                };
        //                _db.RedirectRelation.Add(redirectRelation);
        //                _db.SaveChanges();
        //            }
        //            else
        //            {
        //                //Console.WriteLine($"\tredirect relation already exists in db...");
        //            }
        //        }
        //    }

        //    if (crawlResult.Doc != null)
        //    {
        //        //find links
        //        var links = crawlResult.Doc.DocumentNode.SelectNodes("//a[@href]");
        //        //Console.WriteLine($"\tfound {links.Count} child links.");
        //        var linksToSave = new List<System.Uri>();
        //        foreach (var link in links)
        //        {
        //            var href = link.Attributes["href"].Value;

        //            //if(href.Contains("fortnite-stats/")) Debugger.Break();

        //            if (href == "" || href.StartsWith("javascript:"))
        //                continue;

        //            var decoded = HttpUtility.HtmlDecode(href);
        //            var childUri = Util.GetUriObjectFromUriString(decoded, page.AbsoluteUri);

        //            //no duplicated links
        //            if (linksToSave.All(o => o.AbsoluteUri != childUri.AbsoluteUri))
        //                linksToSave.Add(childUri);
        //        }

        //        Console.WriteLine($"html links parsed {stopWatch.ElapsedMilliseconds}");

        //        //save links & relations to db
        //        if (linksToSave.Count > 0)
        //        {
        //            //Console.WriteLine($"\tthere are {linksToSave.Count} unique links");
        //            //Console.WriteLine($"\tchecking db for existence...");
        //            var lstUri = linksToSave.Select(o => o.AbsoluteUri).ToList();
        //            var pages = _db.Uri.Where(o => lstUri.Contains(o.AbsoluteUri)).ToList();
        //            //Console.WriteLine($"\t{pages.Count} of them are already in db");

        //            Console.WriteLine($"uri pages fetched {stopWatch.ElapsedMilliseconds}");

        //            //save pages
        //            //Console.WriteLine($"\tsaving pages...");
        //            foreach (var linkToSave in linksToSave)
        //            {
        //                var childPage = pages.FirstOrDefault(p => p.AbsoluteUri == linkToSave.AbsoluteUri);
        //                if (childPage == null)
        //                {
        //                    childPage = NewUriDbModel(linkToSave);
        //                    _db.Uri.Add(childPage);
        //                }
        //            }
        //            _db.SaveChanges();

        //            Console.WriteLine($"uri pages saved {stopWatch.ElapsedMilliseconds}");

        //            //save relations
        //            //Console.WriteLine($"\trefetching {lstUri.Count} pages from db...");
        //            pages = _db.Uri.Where(o => lstUri.Contains(o.AbsoluteUri)).ToList();
        //            //Console.WriteLine($"\tchecking existing relations...");
        //            var relations = _db.Relation.Where(o => o.ParentId == page.Id).ToList();
        //            //Console.WriteLine($"\t{relations.Count} of them are already in db");
        //            //Console.WriteLine($"\tsaving relations...");

        //            Console.WriteLine($"relations fetched {stopWatch.ElapsedMilliseconds}");

        //            foreach (var linkToSave in linksToSave)
        //            {
        //                var childPage = pages.FirstOrDefault(p => p.AbsoluteUri == linkToSave.AbsoluteUri);
        //                var relation = relations.FirstOrDefault(o => o.ChildId == childPage.Id);
        //                if (relation == null)
        //                {
        //                    relation = new Relation()
        //                    {
        //                        ParentId = page.Id,
        //                        ChildId = childPage.Id,
        //                        CreatedAt = DateTime.UtcNow,
        //                    };
        //                    _db.Relation.Add(relation);
        //                }
        //            }
        //            _db.SaveChanges();

        //            Console.WriteLine($"relations saved {stopWatch.ElapsedMilliseconds}");
        //        }

        //        //find canonical
        //        var canonicalLinks = crawlResult.Doc.DocumentNode.SelectNodes("//link[@rel='canonical']");
        //        if (canonicalLinks != null && canonicalLinks.Count > 0)
        //        {
        //            var canonicalLinkValue = canonicalLinks[0].Attributes["href"].Value;
        //            //Console.WriteLine($"\tfound canonical");
        //            page.Canonical = canonicalLinkValue;

        //            Console.WriteLine($"canonical extracted {stopWatch.ElapsedMilliseconds}");
        //        }

        //        page.ContentLength = crawlResult.Doc.ParsedText.Length;
        //    }

        //    page.CrawledAt = crawlResult.CrawledAt;
        //    page.StatusCode = crawlResult.StatusCode;
        //    page.StatusCodeString = crawlResult.StatusCodeStr;
        //    page.TimeTaken = crawlResult.TimeTaken;

        //    _db.SaveChanges();

        //    Console.WriteLine($"everything saved {stopWatch.ElapsedMilliseconds}");
        //}

        //private void CrawlPages(CrawlerContext db, string host)
        //{
        //    //var pagesToCrawl = GetPagesToCrawl(db, _hosts);
        //    var pagesToCrawl = new List<Uri>();

        //    Console.WriteLine($"Found {pagesToCrawl.Count} pages to crawl.");
        //    Console.WriteLine();

        //    var web = new HtmlWeb();
        //    web.CaptureRedirect = true;
        //    web.PostResponse = (request, response) => HtmlWeb_PostResponse(request, response);

        //    for (var i = 0; i < pagesToCrawl.Count; i++)
        //    {
        //        var page = pagesToCrawl[i];
        //        Console.WriteLine($"{i + 1}/{pagesToCrawl.Count} {page.AbsoluteUri}");

        //        var stopWatch = new Stopwatch();
        //        stopWatch.Start();

        //        HtmlDocument doc;
        //        try
        //        {
        //            doc = web.Load(page.AbsoluteUri);
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine(e);
        //            page.FailedAt = DateTime.UtcNow;
        //            page.FailedException = e.ToString();
        //            db.SaveChanges();
        //            Console.WriteLine();
        //            continue;
        //        }

        //        stopWatch.Stop();

        //        var statusCode = (int)web.StatusCode;
        //        var statusCodeString = web.StatusCode.ToString();
        //        var timeTaken = stopWatch.Elapsed.TotalSeconds;

        //        //not 200 OK
        //        if (web.StatusCode != HttpStatusCode.OK)
        //        {
        //            Console.WriteLine($"\tstatus code = {statusCode}");

        //            //3xx redirect
        //            if (statusCode / 100 == 3)
        //            {
        //                //Console.WriteLine($"\tfound redirect {statusCode} {_redirectLocation}");
        //                var locationUri = GetUriObjectFromUriString(_redirectLocation, page.AbsoluteUri);
        //                Console.WriteLine($"\tfound new location {locationUri.AbsoluteUri}");

        //                //var linkKey = locationUri.AbsoluteUri.TruncateMax(MAX_URI_LEN);

        //                //add destination to db
        //                var destinationPage = db.Uri.FirstOrDefault(o => o.AbsoluteUri == locationUri.AbsoluteUri);
        //                if (destinationPage == null)
        //                {
        //                    Console.WriteLine($"\tadding destination page to db...");
        //                    destinationPage = NewUriDbModel(locationUri);
        //                    db.Uri.Add(destinationPage);
        //                    db.SaveChanges();
        //                }
        //                else
        //                    Console.WriteLine($"\tdestination page already exists in db...");

        //                //add redirect relations
        //                var redirectRelation = db.RedirectRelation.FirstOrDefault(o =>
        //                    o.SourceId == page.Id && o.DestinationId == destinationPage.Id);
        //                if (redirectRelation == null)
        //                {
        //                    Console.WriteLine($"\tsaving redirect relation to db...");
        //                    redirectRelation = new RedirectRelation()
        //                    {
        //                        SourceId = page.Id,
        //                        DestinationId = destinationPage.Id,
        //                        CreatedAt = DateTime.UtcNow,
        //                    };
        //                    db.RedirectRelation.Add(redirectRelation);
        //                    db.SaveChanges();
        //                }
        //                else
        //                    Console.WriteLine($"\tredirect relation already exists in db...");
        //            }

        //            //save page info
        //            page.CrawledAt = DateTime.UtcNow;
        //            page.StatusCodeString = statusCodeString;
        //            page.StatusCode = statusCode;
        //            page.TimeTaken = (decimal?)timeTaken;
        //            db.SaveChanges();
        //            Console.WriteLine();
        //            continue;
        //        }

        //        //not a document
        //        if (doc.ParsedText == null)
        //        {
        //            page.CrawledAt = DateTime.UtcNow;
        //            page.StatusCodeString = statusCodeString;
        //            page.StatusCode = statusCode;
        //            page.TimeTaken = (decimal?)timeTaken;
        //            db.SaveChanges();
        //            Console.WriteLine();
        //            continue;
        //        }

        //        var links = doc.DocumentNode.SelectNodes("//a[@href]");

        //        Console.WriteLine($"\tfound {links.Count} child links.");

        //        var linksToSave = new List<System.Uri>();
        //        foreach (var link in links)
        //        {
        //            var href = link.Attributes["href"].Value;

        //            if (href == "" || href.StartsWith("javascript:"))
        //                continue;

        //            var decoded = HttpUtility.HtmlDecode(href);
        //            //if (decoded!= href)
        //            //    Debugger.Break();

        //            var childUri = GetUriObjectFromUriString(decoded, page.AbsoluteUri);

        //            //Console.WriteLine($"\t{href}\r\n\t{childUri.AbsoluteUri}");
        //            //Console.WriteLine();

        //            //no duplicated links
        //            if (linksToSave.All(o => o.AbsoluteUri != childUri.AbsoluteUri))
        //                linksToSave.Add(childUri);
        //        }

        //        //save links & relations to db
        //        if (linksToSave.Count > 0)
        //        {
        //            //var uriKeyStrings = linksToSave.Select(o => o.AbsoluteUri.TruncateMax(MAX_URI_LEN)).Distinct().ToList();
        //            Console.WriteLine($"\tthere are {linksToSave.Count} unique links"
        //                              //+$", {uriKeyStrings.Count} unique uri keys"
        //                              );

        //            Console.WriteLine($"\tchecking db for existence...");
        //            var lstUri = linksToSave.Select(o => o.AbsoluteUri).ToList();
        //            var pages = db.Uri.Where(o => lstUri.Contains(o.AbsoluteUri)).ToList();
        //            //var childRelation = db.Relation.Where(o => o.ParentId == page.Id).ToList();
        //            Console.WriteLine($"\t{pages.Count} of them are already in db");

        //            //save pages
        //            Console.WriteLine($"\tsaving pages...");
        //            //var keysAdded = new List<string>();
        //            foreach (var linkToSave in linksToSave)
        //            {
        //                //var uriKey = linkToSave.AbsoluteUri.TruncateMax(MAX_URI_LEN).ToLower();

        //                ////not add uris that are different but with same unique keys
        //                //if (keysAdded.Contains(uriKey))
        //                //    continue;

        //                var childPage = pages.FirstOrDefault(p => p.AbsoluteUri == linkToSave.AbsoluteUri);

        //                if (childPage == null)
        //                {
        //                    childPage = NewUriDbModel(linkToSave);

        //                    //if (linkToSave.AbsoluteUri.Length > MAX_URI_LEN)
        //                    //    childPage.FullAbsoluteUri = linkToSave.AbsoluteUri;

        //                    db.Uri.Add(childPage);
        //                    //keysAdded.Add(uriKey);
        //                }
        //            }
        //            db.SaveChanges();

        //            //save relations
        //            Console.WriteLine($"\trefetching {lstUri.Count} pages from db...");
        //            pages = db.Uri.Where(o => lstUri.Contains(o.AbsoluteUri)).ToList();
        //            Console.WriteLine($"\tchecking existing relations...");
        //            var relations = db.Relation.Where(o => o.ParentId == page.Id).ToList();
        //            Console.WriteLine($"\t{relations.Count} of them are already in db");
        //            Console.WriteLine($"\tsaving relations...");
        //            foreach (var linkToSave in linksToSave)
        //            {
        //                //var uriKey = linkToSave.AbsoluteUri.TruncateMax(MAX_URI_LEN);

        //                var childPage = pages.FirstOrDefault(p => p.AbsoluteUri == linkToSave.AbsoluteUri);
        //                var relation = relations.FirstOrDefault(o => o.ChildId == childPage.Id);
        //                if (relation == null)
        //                {
        //                    relation = new Relation()
        //                    {
        //                        ParentId = page.Id,
        //                        ChildId = childPage.Id,
        //                        CreatedAt = DateTime.UtcNow,
        //                    };
        //                    db.Relation.Add(relation);
        //                }
        //            }

        //            db.SaveChanges();
        //        }

        //        //find canonical
        //        var canonicalLinks = doc.DocumentNode.SelectNodes("//link[@rel='canonical']");
        //        if (canonicalLinks != null && canonicalLinks.Count > 0)
        //        {
        //            var canonicalLinkValue = canonicalLinks[0].Attributes["href"].Value;
        //            Console.WriteLine($"\tfound canonical"
        //                              //+$": {canonicalLinkValue}"
        //                              );
        //            page.Canonical = canonicalLinkValue;
        //        }

        //        //save page info
        //        var contentLength = doc.ParsedText.Length;
        //        var content = doc.ParsedText;
        //        page.CrawledAt = DateTime.UtcNow;
        //        //page.Content = content;
        //        page.ContentLength = contentLength;
        //        page.StatusCodeString = statusCodeString;
        //        page.StatusCode = statusCode;
        //        page.TimeTaken = (decimal?)timeTaken;

        //        db.SaveChanges();

        //        Console.WriteLine();
        //    }
        //}

        private List<CrawlPlan> GetPagesToCrawl(string[] hosts)
        {
            var db = CrawlerContext.Create(_dbConStr);
            Console.WriteLine("loading uncrawled pages...");
            List<CrawlPlan> pagesToCrawl;
            //pagesToCrawl = db.Uri.Where(o => !o.CrawledAt.HasValue
            //                                 && hosts.Contains(o.Host)
            //                                 && o.Fragment == ""
            //                                 && o.Query == ""
            //                                 && !o.AbsoluteUri.Contains("c!")
            //                                 && !o.AbsoluteUri.Contains("i!")
            //                                 && !o.AbsoluteUri.Contains("a!")
            //                                 && !o.AbsoluteUri.Contains("p!")
            //                                 && !o.AbsoluteUri.EndsWith(".png")
            //                                 && !o.AbsoluteUri.EndsWith(".gif")
            //                                 && !o.AbsoluteUri.EndsWith(".jpg")
            //    )
            //    .OrderBy(o => o.Id)
            //    .Select(o => new CrawlPlan()
            //    {
            //        AbsoluteUri = o.AbsoluteUri
            //    })
            //    .ToList();

            //if (pagesToCrawl.Count < 100)
            //{
            //    Console.WriteLine("loading uncrawled pages (2)...");
            //    pagesToCrawl = db.Uri.Where(o => !o.CrawledAt.HasValue
            //                                     && hosts.Contains(o.Host)
            //                                     && o.Fragment == ""
            //                                     && o.Query == ""

            //                                     //&& !o.AbsoluteUri.Contains("c!")
            //                                     //&& !o.AbsoluteUri.Contains("i!")
            //                                     //&& !o.AbsoluteUri.Contains("a!")
            //                                     //&& !o.AbsoluteUri.Contains("p!")
            //                                     && !o.AbsoluteUri.EndsWith(".png")
            //                                     && !o.AbsoluteUri.EndsWith(".gif")
            //                                     && !o.AbsoluteUri.EndsWith(".jpg")
            //        )
            //        .OrderBy(o => o.Id)
            //        .Select(o => new CrawlPlan()
            //        {
            //            AbsoluteUri = o.AbsoluteUri
            //        })
            //        .ToList();
            //}

            //if (pagesToCrawl.Count < 100)
            //{
            //Console.WriteLine("loading uncrawled pages (3)...");


            pagesToCrawl = db.Uri.Where(o => !o.CrawledAt.HasValue
                                             && (o.Scheme == "http" || o.Scheme == "https")
                                             && hosts.Contains(o.Host)
                                             && o.Fragment == ""

                                             //&& o.Query == ""

                                             //&& !o.AbsoluteUri.Contains("c!")
                                             //&& !o.AbsoluteUri.Contains("i!")
                                             //&& !o.AbsoluteUri.Contains("a!")
                                             //&& !o.AbsoluteUri.Contains("p!")
                                             && !o.AbsoluteUri.EndsWith(".png")
                                             && !o.AbsoluteUri.EndsWith(".gif")
                                             && !o.AbsoluteUri.EndsWith(".jpg")
                )
                .OrderBy(o => o.Id)
                .Select(o => new CrawlPlan()
                {
                    AbsoluteUri = o.AbsoluteUri
                })
                .ToList();


            //}

            //recrawl 5xx pages
            //pagesToCrawl = db.Uri.Where(o => o.CrawledAt.HasValue && o.StatusCode.ToString().StartsWith("5")
            //                                 && (o.Scheme == "http" || o.Scheme == "https")
            //                                 && hosts.Contains(o.Host)
            //                                 && o.Fragment == ""

            //                                 //&& o.Query == ""

            //                                 //&& !o.AbsoluteUri.Contains("c!")
            //                                 //&& !o.AbsoluteUri.Contains("i!")
            //                                 //&& !o.AbsoluteUri.Contains("a!")
            //                                 //&& !o.AbsoluteUri.Contains("p!")
            //                                 && !o.AbsoluteUri.EndsWith(".png")
            //                                 && !o.AbsoluteUri.EndsWith(".gif")
            //                                 && !o.AbsoluteUri.EndsWith(".jpg")
            //    )
            //    .OrderBy(o => o.Id)
            //    .Select(o => new CrawlPlan()
            //    {
            //        AbsoluteUri = o.AbsoluteUri
            //    })
            //    .ToList();

            return pagesToCrawl;
        }
        private List<CrawlPlan> GetPagesToBrowserCrawl(string[] hosts)
        {
            var db = CrawlerContext.Create(_dbConStr);
            Console.WriteLine("loading un-browser-crawled pages...");
            List<CrawlPlan> pagesToCrawl;

            pagesToCrawl = db.Uri.Where(o => o.CrawledAt.HasValue && !o.BrowserCrawledAt.HasValue && o.StatusCode==200

                                             && (o.Scheme == "http" || o.Scheme == "https")
                                             && hosts.Contains(o.Host)
                                             && o.Fragment == ""

                                             //&& o.Query == ""

                                             && !o.AbsoluteUri.Contains("c!")
                                             && !o.AbsoluteUri.Contains("i!")
                                             && !o.AbsoluteUri.Contains("a!")
                                             && !o.AbsoluteUri.Contains("p!")

                                             && !o.AbsoluteUri.EndsWith(".png")
                                             && !o.AbsoluteUri.EndsWith(".gif")
                                             && !o.AbsoluteUri.EndsWith(".jpg")
                )
                .OrderBy(o => o.Id)
                .Select(o => new CrawlPlan()
                {
                    AbsoluteUri = o.AbsoluteUri
                })
                .ToList();

            return pagesToCrawl;
        }

        private static Uri NewUriDbModel(System.Uri uri)
        {
            return new Uri()
            {
                AbsoluteUri = uri.AbsoluteUri,//.TruncateMax(MAX_URI_LEN),
                Host = uri.Host,
                AbsolutePath = uri.AbsolutePath,
                Fragment = uri.Fragment,
                Query = uri.Query,
                Scheme = uri.Scheme,
                OriginalString = uri.OriginalString,

                CreateAt = DateTime.UtcNow,
            };
        }
        private static Uri NewUriDbModel(string invalidFormatedUri)
        {
            return new Uri()
            {
                AbsoluteUri = invalidFormatedUri,//.TruncateMax(MAX_URI_LEN),
                //Host = uri.Host,
                //AbsolutePath = uri.AbsolutePath,
                //Fragment = uri.Fragment,
                //Query = uri.Query,
                //Scheme = uri.Scheme,
                //OriginalString = uri.OriginalString,

                CreateAt = DateTime.UtcNow,
            };
        }

        ~LightningCrawler()
        {

        }
    }

    internal class CrawlStatus
    {
    }
}
