using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using Common.ConfigModel;
using Common.CrawlerDbContext;
using Common.ElasticSearchModel;
using Dashboard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NonFactors.Mvc.Grid;
//using GridConfig = Dashboard.Models.GridConfig;
using Uri = Common.CrawlerDbContext.Uri;

namespace Dashboard.Controllers
{
    public class SearchController : Controller
    {
        private readonly ILogger<SearchController> _logger;
        private readonly CrawlerContext db;
        private readonly EsConnectionOptions _esConnectionOptions;

        public SearchController(ILogger<SearchController> logger, CrawlerContext db, EsConnectionOptions esConnectionOptions)
        {
            _logger = logger;
            this.db = db;
            _esConnectionOptions = esConnectionOptions;
        }
        // GET: UriPage
        public ActionResult Index(string keyword)
        {
            return View("Index", keyword);
        }

        public ActionResult GridAjax(string keyword, Int32? page=1, Int32? rows=20)
        {
            var settings = new ConnectionSettings(new System.Uri(_esConnectionOptions.Host))
                .DefaultIndex("uri").BasicAuthentication(_esConnectionOptions.Username, _esConnectionOptions.Password);
            var elasticClient = new ElasticClient(settings);

            var searchResponse = elasticClient.Search<UriDocument>(s => s
                //.Query(q =>q
                //    .Match(m=>m.Query(keyword).Field(o=>o.BrowserText))
                //)
                .Query(q =>
                    q.Match(m => m.Query(keyword).Field(o => o.AbsoluteUri))
                    || q.Match(m => m.Query(keyword).Field(o => o.BrowserText))
                    || q.Match(m => m.Query(keyword).Field(o => o.BrowserHtml))
                    || q.Match(m => m.Query(keyword).Field(o => o.OriginalUriString))
                )
                //.Explain(true)
                //.Sort(sort=>sort.Descending(i=>i))
                .From(rows*(page-1))
                .Size(rows)
                //.StoredFields()
                .Highlight(h => h
                    .PreTags("1vAn1Saw3s0me")
                    .PostTags("em0s3waS1nAv1")
                    .Fields(
                        f => f.Field(o => o.AbsoluteUri)
                        ,f => f.Field(o => o.BrowserText)
                        ,f => f.Field(o => o.BrowserHtml)
                    )
                )
            );

            //foreach (var hit in searchResponse.Hits)
            //{
            //    var aggregate = hit.Highlight.Values
            //        .Select(o => o.Aggregate((a, b) => a + "<br/>" + b))
            //        .Aggregate((o, n) => o + "<br/><br/>" + n);
            //}

            var total = searchResponse.Total;
            var hits = searchResponse.Hits.Select(o => o);
            var searchResponseHitsMetadata = searchResponse.HitsMetadata;

            var model = new SearchPageDto();
            model.Result = hits.Select(h =>
            {
                var item= new SearchUri
                {
                    Uri = h.Source.AbsoluteUri,
                    Highlight = h.Highlight.Any()
                        ? h.Highlight.Select(o =>
                                o.Key== "browserHtml"
                                ? o.Value.Select(v=> HttpUtility.HtmlEncode(v)).Aggregate((a, b) => a + "<br/>" + b)
                                : o.Value.Aggregate((a, b) => a + "<br/>" + b)
                                )
                            .Aggregate((o, n) => o + "<hr/>" + n)
                        : "",
                    Score = h.Score.Value
                };

                item.Highlight = item.Highlight
                    .Replace("1vAn1Saw3s0me", "<em>")
                    .Replace("em0s3waS1nAv1", "</em>");
                    //.Replace("&lt;1van1saw3s0me&gt;", "<em>")
                    //.Replace("&lt;/1van1saw3s0me&gt;", "</em>");

                return item;
            }).ToList();

            model.Total = (int) total;

            var base64 = Request.Cookies["gridColumnSetting_search"];
            if (base64 != null)
            {
                byte[] decodedBytes = Convert.FromBase64String(base64);
                string colSetting = System.Text.Encoding.UTF8.GetString(decodedBytes);
                var gridConfig = JsonConvert.DeserializeObject<GridConfig>(colSetting);
                //deserializeObject.columns.Select(o => o.name).ToList();

                ViewBag.gridConfig = gridConfig;
            }

            return PartialView("_SearchGridPartial", model);
        }
    }
}