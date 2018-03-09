using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Plugins.Databases.NameSourceDetectors;

namespace Graphode.CodeAnalyzer.Plugins.Databases.Finder
{
    public class LocalAppConfigFinder : IDatabaseFinder
    {
        private string _lastLoadedForApp;
        private Dictionary<string, IndexedList<string, string>> _appSettings;
        private Dictionary<string, IndexedList<string, string>> _connStringDatabaseNames;
        private ISourceDetector _appConfigSourceDetector;

        public LocalAppConfigFinder()
        {
            _appConfigSourceDetector = new AppConfigSourceDetector();
            _lastLoadedForApp = string.Empty;
        }

        public string FindDatabaseName(TargetMethodMatch targetMethodMatch, MethodCall methodCall, MethodObject currentMethod, IAssignmentGraphWalker assignmentGraphWalker)
        {
            if (!_lastLoadedForApp.Equals(AnalysisScope.CurrentApplicationName))
            {
                IndexAppConfig();
                _lastLoadedForApp = AnalysisScope.CurrentApplicationName;
            }

            var assemblyName = currentMethod.GetMethodDefinition().Module.Assembly.Name.Name;

            // strategy 1 - if application accesses only one db, then choose that
            var appDbs = GetConnectionStrings(AnalysisScope.CurrentApplicationName);
            if (appDbs.Indexes.Count() == 1)
            {
                var indexedDb = appDbs.IndexValues.First();
                return indexedDb.First(); // TODO: if multiple app.configs contains the same db, then here you might want to choose which one you want
            }
            
            // strategy 2 - if the assembly has an app config with only 1 connection string
            var assDbs = GetConnectionStrings(assemblyName);
            if (assDbs.Indexes.Count() == 1)
            {
                var indexedDb = assDbs.IndexValues.First();
                return indexedDb.First(); // TODO: if multiple app.configs contains the same db, then here you might want to choose which one you want
            }

            // strategy 3 - if the database access is ADO.NET then use backtracking search to find the conn string name
            if(targetMethodMatch.AccessMode.IndexOf("ado.net", StringComparison.OrdinalIgnoreCase) > -1)
            {
                var matches = PerformAdoNetBacktrackingSearch(methodCall, assignmentGraphWalker);
                if (matches.Count == 1)
                    return matches.First();
            }

            // strategy 4 - if EF then find the ctor of the context and do a backtracking search from there
            // TODO

            // there are multiple possible matches or none at all
            return "Unknown";
        }

        #region app config indexing 

        private void IndexAppConfig()
        {
            _appSettings = new Dictionary<string, IndexedList<string, string>>();
            _connStringDatabaseNames = new Dictionary<string, IndexedList<string, string>>();

            foreach (var assembly in AnalysisScope.ApplicationAssemblies)
            {
                var assemblyName = assembly.Replace(".dll", "");
                var appConfigs = LoadAssemblyAppConfig(assemblyName);
                _appSettings.Add(assemblyName, new IndexedList<string, string>());
                _connStringDatabaseNames.Add(assemblyName, new IndexedList<string, string>());
                IndexAppSettings(assemblyName, appConfigs);
                IndexConnStrings(assemblyName, appConfigs);
            }
        }

        private void IndexAppSettings(string assemblyName, List<XDocument> appConfigs)
        {
            foreach(var appConfig in appConfigs)
            {
                if (appConfig.Root.Name.LocalName.Equals("configuration", StringComparison.OrdinalIgnoreCase))
                {
                    var appSettings = appConfig.Root.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("appSettings", StringComparison.OrdinalIgnoreCase));
                    if (appSettings != null)
                    {
                        foreach (var element in appSettings.Elements())
                        {
                            if (element.Name.LocalName.Equals("add", StringComparison.OrdinalIgnoreCase)
                                && element.Attributes().Any(x => x.Name.LocalName.Equals("name", StringComparison.OrdinalIgnoreCase))
                                && element.Attributes().Any(x => x.Name.LocalName.Equals("value", StringComparison.OrdinalIgnoreCase)))
                            {
                                var name = element.Attributes().First(x => x.Name.LocalName.Equals("name", StringComparison.OrdinalIgnoreCase)).Value;
                                var value = element.Attributes().First(x => x.Name.LocalName.Equals("value", StringComparison.OrdinalIgnoreCase)).Value.Trim();

                                _appSettings[assemblyName].Add(name, value);

                                if (value.IndexOf("Initial Catalog", StringComparison.OrdinalIgnoreCase) > -1 && value.IndexOf("Data source", StringComparison.OrdinalIgnoreCase) > -1)
                                {
                                    var connStringParts = value.Split(';');
                                    foreach (var part in connStringParts)
                                    {
                                        var dbPart = part.Trim();
                                        if (dbPart.StartsWith("Initial Catalog", StringComparison.OrdinalIgnoreCase) || dbPart.StartsWith("Database", StringComparison.OrdinalIgnoreCase))
                                        {
                                            var dbname = dbPart.Split('=')[1];
                                            _connStringDatabaseNames[assemblyName].Add(name, value);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void IndexConnStrings(string assemblyName, List<XDocument> appConfigs)
        {
            foreach (var appConfig in appConfigs)
            {
                var connStrings = appConfig.Root.Elements().FirstOrDefault(x => x.Name.LocalName.Equals("connectionStrings", StringComparison.OrdinalIgnoreCase));
                if (connStrings != null)
                {
                    foreach (var element in connStrings.Elements())
                    {
                        if (element.Name.LocalName.Equals("add", StringComparison.OrdinalIgnoreCase)
                            && element.Attributes().Any(x => x.Name.LocalName.Equals("connectionString", StringComparison.OrdinalIgnoreCase)))
                        {
                            var name = element.Attributes().First(x => x.Name.LocalName.Equals("name", StringComparison.OrdinalIgnoreCase)).Value;
                            var connStr = element.Attributes().First(x => x.Name.LocalName.Equals("connectionString", StringComparison.OrdinalIgnoreCase)).Value;

                            string dbName = string.Empty;
                            var connStringParts = connStr.Split(';');
                            foreach (var part in connStringParts)
                            {
                                var dbPart = part.Trim();
                                if (dbPart.StartsWith("Initial Catalog", StringComparison.OrdinalIgnoreCase) || dbPart.StartsWith("Database", StringComparison.OrdinalIgnoreCase))
                                {
                                    dbName = dbPart.Split('=')[1];
                                    _connStringDatabaseNames[assemblyName].Add(name, dbName);
                                }
                            }
                        }
                    }
                }
            }
        }

        private XDocument LoadApplicationAppConfig()
        {
            var appConfigPath = Directory.GetFiles(AnalysisScope.CurrentApplicationPath).FirstOrDefault(x => x.Equals("app.config", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(appConfigPath))
            {
                return XDocument.Load(appConfigPath);
            }

            return null;
        }

        private List<XDocument> LoadAssemblyAppConfig(string assemblyName)
        {
            var appConfigs = new List<XDocument>();
            var assemblyPath = FindAssemblyDirectoryBfs(AnalysisScope.CurrentSolutionPath, assemblyName);
            var appConfigPaths = Directory.GetFiles(assemblyPath)
                                            .Where(x => x.EndsWith("app.config", StringComparison.OrdinalIgnoreCase)
                                                || x.EndsWith("web.config", StringComparison.OrdinalIgnoreCase))
                                            .ToList();

            foreach(var appConfigPath in appConfigPaths)
                appConfigs.Add(XDocument.Load(appConfigPath));

            return appConfigs;
        }

        public static string FindAssemblyDirectoryBfs(string path, string assemblyName)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                var dir = new DirectoryInfo(path);
                if (dir.Name.Equals(assemblyName))
                    return path;

                foreach (string subDir in Directory.GetDirectories(path))
                    queue.Enqueue(subDir);
            }

            return string.Empty;
        }

        #endregion

        private IndexedList<string,string> GetConnectionStrings(string assemblyName)
        {
            if(_connStringDatabaseNames.ContainsKey(assemblyName))
            {
                return _connStringDatabaseNames[assemblyName];
            }

            return new IndexedList<string,string>();
        }

        private IndexedList<string, string> GetAppSettings(string assemblyName)
        {
            if (_connStringDatabaseNames.ContainsKey(assemblyName))
            {
                return _appSettings[assemblyName];
            }

            return new IndexedList<string, string>();
        }

        private List<string> PerformAdoNetBacktrackingSearch(MethodCall method, IAssignmentGraphWalker assignmentGraphWalker)
        {
            // these tell the backtracking search that it can follow the instantiation (ctor) of the below classes in its search
            var permittedGoToInstancePatterns = new List<GoToInstancePattern>();
            permittedGoToInstancePatterns.Add(new GoToInstancePattern() { TypeMustHavePattern = "System.Data.Common.DbCommand", MemberMustNotHavePattern = ".ctor", TryInstance = TryInstance.First }); // ADO.NET
            permittedGoToInstancePatterns.Add(new GoToInstancePattern() { TypeMustHavePattern = "System.Data.SqlClient.SqlCommand", MemberMustNotHavePattern = ".ctor", TryInstance = TryInstance.First }); // ADO.NET
            permittedGoToInstancePatterns.Add(new GoToInstancePattern() { TypeMustHavePattern = "System.Data.Common.DbConnection", MemberMustNotHavePattern = ".ctor", TryInstance = TryInstance.First });
            permittedGoToInstancePatterns.Add(new GoToInstancePattern() { TypeMustHavePattern = "System.Data.SqlClient.SqlConnection", MemberMustNotHavePattern = ".ctor", TryInstance = TryInstance.First });
            permittedGoToInstancePatterns.Add(new GoToInstancePattern() { TypeMustHavePattern = "System.Data.SqlClient.SqlBulkCopy", MemberMustNotHavePattern = ".ctor", TryInstance = TryInstance.First }); // SqlBulkCopy

            // can be useful when using a library where you create derived classes
            var searchBaseConstructorPatterns = new List<string>();

            var results = assignmentGraphWalker.PerformBacktrackingSearch(method.Instruction,
                method.OwnerMethod,
                _appConfigSourceDetector,
                permittedGoToInstancePatterns,
                searchBaseConstructorPatterns);

            var sources = new List<string>();
            foreach (var result in results)
            {
                foreach (var node in result.FoundNodes)
                {
                    if (node.Triple.To.ObjectKey.Equals("System.Configuration.ConfigurationManager.ConnectionStringSettingsCollection::[System.String]>>1"))
                    {
                        var dbsOfAssembly = GetConnectionStrings(node.Triple.ParentAssembly);
                        var database = dbsOfAssembly.Get(node.Triple.From.ObjectKey).FirstOrDefault(); // TODO: if there is more than one that matches you might want to choose it here
                        if (!string.IsNullOrEmpty(database))
                        {
                            sources.Add(database);
                        }
                        else
                        {
                            var dbsOfApplication = GetConnectionStrings(AnalysisScope.CurrentApplicationName);
                            database = dbsOfApplication.Get(node.Triple.From.ObjectKey).FirstOrDefault(); // TODO: if there is more than one that matches you might want to choose it here
                            if (!string.IsNullOrEmpty(database))
                                sources.Add(database);
                        }
                    }
                    else
                    {
                        // currently this finder only supports the ConnectionStringSettingsCollection
                    }
                }
            }

            return sources;
        }
    }
}
