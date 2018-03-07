using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.Common;

namespace Graphode.CodeAnalyzer.Contracts.Common
{
    public interface IAnalysisLogger
    {
        void Log(LogEntrySeverity severity, string logSource, string message, string codeSource, LogEntryType entryType);
    }
}
