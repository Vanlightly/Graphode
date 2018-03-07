using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Graphode.CodeAnalyzer.Implementations.Common
{
    public class AssemblyPathFinder
    {
        public static List<string> GetApplicationFolders(string rootPath, string applicationsPattern)
        {
            var folders = Directory.GetDirectories(rootPath);
            var patterns = applicationsPattern.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var applicationFolders = new List<string>();
            foreach(var folder in folders)
            {
                foreach(var pattern in patterns)
                {
                    if(Regex.IsMatch(folder, pattern))
                    {
                        applicationFolders.Add(folder);
                        break;
                    }
                }
            }

            return applicationFolders;
        }

        public static string GetAssembliesFolder(string projectFolder)
        {
            var binFolder = Directory.GetDirectories(projectFolder).FirstOrDefault(x => x.EndsWith("bin", StringComparison.OrdinalIgnoreCase));
            if(binFolder == null)
            {
                return null;
            }

            var debugFolder = Directory.GetDirectories(binFolder).FirstOrDefault(x => x.EndsWith("debug", StringComparison.OrdinalIgnoreCase));
            var releaseFolder = Directory.GetDirectories(binFolder).FirstOrDefault(x => x.EndsWith("release", StringComparison.OrdinalIgnoreCase));

            if (debugFolder != null)
                return debugFolder;

            if (releaseFolder != null)
                return releaseFolder;

            return binFolder;
        }

        public static List<string> GetCompanyAssemblies(string companyAssemblyPattern, string folder)
        {
            var assemblies = Directory.GetFiles(folder).Where(x => x.EndsWith("dll") || x.EndsWith("exe")).ToList();
            var assemblyNames = assemblies.Select(x => Path.GetFileName(x));
            return assemblyNames.Where(x => Regex.IsMatch(x, companyAssemblyPattern)).ToList();
        }
    }
}
