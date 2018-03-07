using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities.Common;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    // singleinstance!!
    public class AnalysisLogger : IAnalysisLogger
    {
        private static string _filePath;
        private object _syncObj = new object();

        static AnalysisLogger()
        {
            //_filePath = @"C:\Temp\MapLog\AnalysisLog" + DateTime.Now.ToString("yyyy_mm_dd_hh_mm_ss") + ".txt";
            //File.WriteAllText(_filePath, "Severity,EntryType,Assembly,Source,Message" + Environment.NewLine);
        }

        public void Log(LogEntrySeverity severity, string source, string message, string assemblyName, LogEntryType entryType)
        {
            //var line = severity + "," + CsvWriter.Escape(assemblyName) + "," + entryType + "," + CsvWriter.Escape(source) + "," + CsvWriter.Escape(message) + Environment.NewLine;

            //lock (_syncObj)
            //{
                //File.AppendAllText(_filePath, line);
            //}
        }
    }
}
