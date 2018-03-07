using System;

namespace Graphode.CodeAnalyzer.Logging
{
    public interface ILogOutput
    {
        void ApplicationAnalisisComplete(string application, string status);
        void ApplicationLoadComplete(string application, string status);
        void LogInfo(string text);
        void LogError(string text);
        void LogError(string text, Exception ex);
        void LogAnalysis(string text);
    }
}
