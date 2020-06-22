using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Util
{
    public static class StringExtensions
    {
        public static string TruncateMax(this string source, int length)
        {
            if (source == null)
            {
                return null;
            }
            if (source.Length > length)
            {
                return source.Substring(0, length);
            }
            return source;
        }
    }
}
