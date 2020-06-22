using System;
using System.Collections.Generic;

namespace Common.CrawlerDbContext
{
    public partial class RedirectRelation
    {
        public int SourceId { get; set; }
        public int DestinationId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
