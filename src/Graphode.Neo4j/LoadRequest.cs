using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphode.Neo4j
{
    public enum Locality
    {
        Local,
        Remote
    }

    public class LoadRequest
    {
        public string Neo4jUrl { get; set; }
        public string FtpBaseUrl { get; set; }
        public string NeoCsvFolder { get; set; }
        public Locality Locality { get; set; }
        public string ApplicationName { get; set; }
        public string GraphType { get; set; }
    }
}
