using System;
using System.Collections.Generic;

namespace Common.CrawlerDbContext
{
    public partial class Relation
    {
        public int ParentId { get; set; }
        public int ChildId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
