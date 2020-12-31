using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ConfigModel
{
    public class EsConnectionOptions
    {
        public const string EsConnection = "EsConnection";

        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
