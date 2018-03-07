using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Plugins.Databases.NameSourceDetectors
{
    public class AppConfigSourceDetector : ISourceDetector
    {
        private const string ConfigManagerConnStrings = "System.Configuration.ConfigurationManager.ConnectionStringSettingsCollection::[System.String]>>1";
        
        public bool IsNameSource(Triple triple)
        {
            return triple.To.ObjectKey.Equals(ConfigManagerConnStrings);
        }
    }
}
