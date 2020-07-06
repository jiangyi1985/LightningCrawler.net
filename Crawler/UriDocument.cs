﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Crawler
{
    public class UriDocument
    {
        public int Id { get; set; }
        public string AbsoluteUri { get; set; }
        public string OriginalUriString { get; set; }
        public string BrowserHtml { get; set; }
        public string BrowserText { get; set; }
    }
}
