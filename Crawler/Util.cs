using System;
using System.Collections.Generic;
using System.Text;

namespace Crawler
{
    public class Util
    {
        public static System.Uri GetUriObjectFromUriString(string uriString, string baseAbsoluteUri)
        {
            var childUri = new System.Uri(uriString, UriKind.RelativeOrAbsolute);

            if (!childUri.IsAbsoluteUri)
            {
                childUri = new System.Uri(new System.Uri(baseAbsoluteUri), childUri);
            }

            return childUri;
        }
    }
}
