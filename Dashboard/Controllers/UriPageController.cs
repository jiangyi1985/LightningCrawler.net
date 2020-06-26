using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.CrawlerDbContext;
using Dashboard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NonFactors.Mvc.Grid;
//using GridConfig = Dashboard.Models.GridConfig;
using Uri = Common.CrawlerDbContext.Uri;

namespace Dashboard.Controllers
{
    public class UriPageController : Controller
    {
        private readonly ILogger<UriPageController> _logger;
        private readonly CrawlerContext db;

        public UriPageController(ILogger<UriPageController> logger, CrawlerContext db)
        {
            _logger = logger;
            this.db = db;
        }
        // GET: UriPage
        public ActionResult Index(int? parentId = null, int? childId = null)
        {
            //var db = CrawlerContext.Create();
            //var db=new CrawlerContext();

            string headerText;

            if (parentId.HasValue)
            {
                var page = db.Uri.FirstOrDefault(o => o.Id == parentId);
                headerText = $"Link Relation: Links on page <a href=\"{page.AbsoluteUri}\" target=\"_blank\">{page.AbsoluteUri}</a>";
            }
            else if (childId.HasValue)
            {
                var page = db.Uri.FirstOrDefault(o => o.Id == childId);
                headerText = $"Link Relation: Pages that has a link to <a href=\"{page.AbsoluteUri}\" target=\"_blank\">{page.AbsoluteUri}</a>";
            }
            else
                headerText = "URL";

            return View("Grid", new UriPageListIndexDto
            {
                ParentId = parentId,
                ChildId = childId,
                HeaderText = headerText,
            });
        }

        // GET: UriPage
        public ActionResult GridAjax(int? parentId = null, int? childId = null)
        {
            //var db = CrawlerContext.Create();

            IQueryable<UriPageDto> query;

            if (parentId.HasValue)
                query = from relation in db.Relation
                    join uri in db.Uri on relation.ChildId equals uri.Id
                    where relation.ParentId == parentId
                    select new UriPageDto()
                    {
                        Id = uri.Id,
                        AbsoluteUri = uri.AbsoluteUri,
                        CreateAt = uri.CreateAt, // == null ? uri.CreateAt : uri.CreateAt.Value.ToLocalTime(),
                        StatusCode = uri.StatusCode,
                        CrawledAt = uri.CrawledAt, // == null ? uri.CrawledAt : uri.CrawledAt.Value.ToLocalTime(),
                        BrowserCrawledAt = uri.BrowserCrawledAt,
                        ContentLength = uri.ContentLength,
                        Fragment = uri.Fragment,
                        Query = uri.Query,
                        Host = uri.Host,
                        TimeTaken = uri.TimeTaken,
                        FailedAt = uri.FailedAt, // == null ? uri.FailedAt : uri.FailedAt.Value.ToLocalTime(),
                        FailedException = uri.FailedException,
                        BrowserFailedAt = uri.BrowserFailedAt,
                        BrowserFailedException = uri.BrowserFailedException,
                        HasLinks = db.Relation.Count(r => r.ParentId == uri.Id),
                        FromLinks = db.Relation.Count(r => r.ChildId == uri.Id),
                        IsBrowserRequired = relation.IsBrowserRequired,
                    };
            else if (childId.HasValue)
                query = from relation in db.Relation
                    join uri in db.Uri on relation.ParentId equals uri.Id
                    where relation.ChildId == childId
                    select new UriPageDto()
                    {
                        Id = uri.Id,
                        AbsoluteUri = uri.AbsoluteUri,
                        CreateAt = uri.CreateAt, // == null ? uri.CreateAt : uri.CreateAt.Value.ToLocalTime(),
                        StatusCode = uri.StatusCode,
                        CrawledAt = uri.CrawledAt, // == null ? uri.CrawledAt : uri.CrawledAt.Value.ToLocalTime(),
                        BrowserCrawledAt = uri.BrowserCrawledAt,
                        ContentLength = uri.ContentLength,
                        Fragment = uri.Fragment,
                        Query = uri.Query,
                        Host = uri.Host,
                        TimeTaken = uri.TimeTaken,
                        FailedAt = uri.FailedAt, // == null ? uri.FailedAt : uri.FailedAt.Value.ToLocalTime(),
                        FailedException = uri.FailedException,
                        BrowserFailedAt = uri.BrowserFailedAt, 
                        BrowserFailedException = uri.BrowserFailedException,
                        HasLinks = db.Relation.Count(r => r.ParentId == uri.Id),
                        FromLinks = db.Relation.Count(r => r.ChildId == uri.Id),
                        IsBrowserRequired = relation.IsBrowserRequired,
                    };
            else
                query = from uri in db.Uri
                    select new UriPageDto()
                    {
                        Id = uri.Id,
                        AbsoluteUri = uri.AbsoluteUri,
                        CreateAt = uri.CreateAt, // == null ? uri.CreateAt : uri.CreateAt.Value.ToLocalTime(),
                        StatusCode = uri.StatusCode,
                        CrawledAt = uri.CrawledAt, // == null ? uri.CrawledAt : uri.CrawledAt.Value.ToLocalTime(),
                        BrowserCrawledAt = uri.BrowserCrawledAt,
                        ContentLength = uri.ContentLength,
                        Fragment = uri.Fragment,
                        Query = uri.Query,
                        Host = uri.Host,
                        TimeTaken = uri.TimeTaken,
                        FailedAt = uri.FailedAt, // == null ? uri.FailedAt : uri.FailedAt.Value.ToLocalTime(),
                        FailedException = uri.FailedException,
                        BrowserFailedAt = uri.BrowserFailedAt,
                        BrowserFailedException = uri.BrowserFailedException,
                        HasLinks = db.Relation.Count(r => r.ParentId == uri.Id),
                        FromLinks = db.Relation.Count(r => r.ChildId == uri.Id),
                        //Location = db.RedirectRelation.Any(r => r.SourceId == uri.Id)
                        //    ? db.Uri.FirstOrDefault(o =>
                        //            o.Id == db.RedirectRelation.FirstOrDefault(r => r.SourceId == uri.Id).DestinationId)
                        //        .AbsoluteUri
                        //    : null
                        IsBrowserRequired = null,
                    };

            //var list = query.ToList();

            var base64 = Request.Cookies["gridColumnSetting"];
            if (base64 != null)
            {
                byte[] decodedBytes = Convert.FromBase64String(base64);
                string colSetting = System.Text.Encoding.UTF8.GetString(decodedBytes);
                var gridConfig = JsonConvert.DeserializeObject<GridConfig>(colSetting);
                //deserializeObject.columns.Select(o => o.name).ToList();

                ViewBag.gridConfig = gridConfig;
            }

            return PartialView("_GridAjax", query.OrderByDescending(o=>o.FromLinks));
        }

// GET: UriPage/Details/5
public ActionResult Details(int id)
        {
            return View();
        }

        // GET: UriPage/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UriPage/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UriPage/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UriPage/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UriPage/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UriPage/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}