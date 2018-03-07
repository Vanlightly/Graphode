using System;
using System.Configuration;
using System.IO;

namespace Graphode.CodeAnalyzer.Logging
{
    public enum StdOutMode
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Error = 3
    }

    public class LogOutput : ILogOutput
    {
        public static StdOutMode Mode = StdOutMode.Error;
        
        public void ApplicationAnalisisComplete(string application, string status)
        {
            var message = DateTime.Now.ToString("HH:mm:ss") + "," + application + "," + status + Environment.NewLine;
            //File.AppendAllText(_applicationsLogFile, message);
            Console.WriteLine(message);
        }

        public void ApplicationLoadComplete(string application, string status)
        {
            var message = DateTime.Now.ToString("HH:mm:ss") + "," + application + "," + status + Environment.NewLine;
            //File.AppendAllText(_dataLoadLogFile, message);
            Console.WriteLine(message);
        }

        public void LogError(string text)
        {
            if (Mode == StdOutMode.Error)
            {
                string message = DateTime.Now.ToString("HH:mm:ss") + " [ERROR] " + text + Environment.NewLine;
                Console.WriteLine(message);
            }
        }

        public void LogError(string text, Exception ex)
        {
            if (Mode == StdOutMode.Error)
            {
                string message = DateTime.Now.ToString("HH:mm:ss") + " [ERROR] " + text + " " + ex.ToString() + Environment.NewLine;
                Console.WriteLine(message);
            }
        }

        public void LogInfo(string text)
        {
            if ((int)Mode >= 2)
            {
                var message = DateTime.Now.ToString("HH:mm:ss") + " [INFO] " + text + Environment.NewLine;
                Console.WriteLine(message);
            }
        }

        public void LogAnalysis(string text)
        {
            if ((int)Mode >= 1)
                Console.WriteLine(text);
        }

        public static void LogAnalysisMessage(string text)
        {
            if ((int)Mode >= 1)
                Console.WriteLine(text);
        }
    }
}
