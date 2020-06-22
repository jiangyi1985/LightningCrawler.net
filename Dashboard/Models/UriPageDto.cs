using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public class UriPageDto:Common.CrawlerDbContext.Uri
    {
        public int HasLinks { get; set; }
        public int FromLinks { get; set; }
    }
    public class UriPageListIndexDto 
    {
        public string HeaderText { get; set; }
        public int? ParentId { get; set; }
        public int? ChildId { get; set; }
    }
}
