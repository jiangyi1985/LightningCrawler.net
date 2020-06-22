using System;
using System.Collections.Generic;

namespace Common.CrawlerDbContext
{
    public partial class Uri
    {
        public int Id { get; set; }
        public string AbsoluteUri { get; set; }
        public string AbsolutePath { get; set; }
        public string Host { get; set; }
        public string Scheme { get; set; }
        public string Fragment { get; set; }
        public string Query { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public string FailedException { get; set; }
        public DateTime? CrawledAt { get; set; }
        public decimal? TimeTaken { get; set; }
        public int? StatusCode { get; set; }
        public string StatusCodeString { get; set; }
        public int? ContentLength { get; set; }
        public string Content { get; set; }
        public string Canonical { get; set; }
        public string OriginalString { get; set; }
    }
}
