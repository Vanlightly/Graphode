using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Graphode.CodeAnalyzer.Entities.Configuration
{
    public class Iom
    {
        public string Path { get; set; }
        public XDocument Contents { get; set; }
    }
}
