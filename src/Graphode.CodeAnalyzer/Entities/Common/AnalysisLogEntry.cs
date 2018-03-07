using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.Common
{
    public class AnalysisLogEntry
    {
        public string Message { get; set; }
        public string AssemblyName { get; set; }
        public LogEntryType EntryType { get; set; }
    }
}
