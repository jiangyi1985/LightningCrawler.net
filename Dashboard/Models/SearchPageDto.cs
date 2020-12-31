using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public class SearchPageDto//:Common.ElasticSearchModel.UriDocument
    {
       //public int Id { get; set; }
       public List<SearchUri> Result { get; set; }
       public int Total { get; set; }
    }

    public class SearchUri
    {
        public string Uri { get; set; }
        public string Highlight { get; set; }
        public double Score { get; set; }
    }
}
