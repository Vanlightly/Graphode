using System;
using System.IO;
using System.Collections.Generic;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;
using Graphode.CodeAnalyzer.Plugins.Databases.Detectors;
using Graphode.CodeAnalyzer.Plugins.Databases.Finder;
using Graphode.CodeAnalyzer;
using Graphode.Neo4j;
using System.Linq;

namespace Graphode.TestHarness
{
    class Program
    {
        static Dictionary<string, string> ArgumentsLookup = new Dictionary<string, string>()
        {
            { "--loadmode", "loadmode" }, { "-lm", "loadmode" },
            { "--solution-path", "solution-path" }, { "-sp", "solution-path" },
            { "--output-path", "output-path" }, { "-op", "output-path" },
            { "--locality", "locality" }, { "-l", "locality" },
            { "--neo4j-url", "neo4j-url" }, { "-n", "neo4j-url" },
            { "--remote-csv-path", "remote-csv-path" }, { "-rp", "remote-csv-path" },
            { "--just-my-code-pattern", "just-my-code-pattern" }, { "-j", "just-my-code-pattern" },
            { "--applications-pattern", "applications-pattern" }, { "-ap", "applications-pattern" }
        };


        /// <summary>
        /// arguments:
        /// --loadmode -lm: analyze, analyze_load, load
        /// --solution-path -sp: [a valid file system path]
        /// --output-path -op: [a valid file system path]
        /// --locality -l: local or remote
        /// --neo4j-url -n: for example http://neo4j:12345@localhost:7474/db/data
        /// --remote-csv-path -rp: [a valid file system path on Neo4j server, in Neo4j Cypher format]
        /// --just-my-code-pattern -j: [Regex pattern to identify assemblies to be analyzed]
        /// --applications-pattern -ap: [Regex pattern to identify projects which are applications]
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"Arguments: 
--loadmode -lm: analyze, analyze_load, load
--solution-path -sp: [a valid file system path]
--output-path -op: [a valid file system path]
--locality -l: local or remote
--neo4j-url -n: for example http://neo4j:12345@localhost:7474/db/data
--remote-csv-path -rp: [a valid file system path on Neo4j server, in Neo4j Cypher format]
--just-my-code-pattern -j: [Regex pattern to identify assemblies to be analyzed]
--applications-pattern -ap: [Regex pattern to identify projects which are applications]
");
                var lineArgs = System.Console.ReadLine();
                // TODO: proper tokenization here is needed
                args = lineArgs.Split(' ');
            }

            var arguments = args.ToList();

            Dictionary<string, string> argDict = new Dictionary<string, string>();
            for(int i=0; i<arguments.Count; i+=2)
            {
                string argument = arguments[i];
                string value = arguments[i+1];

                if (ArgumentsLookup.ContainsKey(argument))
                {
                    argDict.Add(ArgumentsLookup[argument], value);
                }
                else
                {
                    // error
                }
            }

            string loadMode = string.Empty;
            if (!argDict.TryGetValue("loadmode", out loadMode))
            {
                Console.WriteLine("Load mode not specified (--loadmode or -lm)");
                return;
            }

            string solutionFolder = string.Empty;
            if (!argDict.TryGetValue("solution-path", out solutionFolder))
            {
                Console.WriteLine("The solution path was not given (--solution-path or -sp)");
                return;
            }

            string outputFolder = string.Empty;
            if (!argDict.TryGetValue("output-path", out outputFolder))
            {
                Console.WriteLine("The output csv path was not given (--output-path or -op)");
                return;
            }

            string justMyCodePattern = string.Empty;
            if (!argDict.TryGetValue("just-my-code-pattern", out justMyCodePattern))
            {
                Console.WriteLine("The just my code regex pattern was not given (--just-my-code-pattern or -j)");
                return;
            }

            string applicationsPattern = string.Empty;
            if (!argDict.TryGetValue("applications-pattern", out applicationsPattern))
            {
                Console.WriteLine("The application project regex pattern was not given (--applications-pattern or -ap)");
                return;
            }

            var p = new Program();

            if (loadMode.Equals("analyze_load", StringComparison.OrdinalIgnoreCase))
            {
                string neo4jUrl = string.Empty;
                if (!argDict.TryGetValue("neo4j-url", out neo4jUrl))
                {
                    Console.WriteLine("The Neo4j url was not given (--neo4j-url or -n)");
                    return;
                }

                bool isRemote = false;
                string locality = string.Empty;
                if (!argDict.TryGetValue("locality", out locality))
                {
                    Console.WriteLine("The locality mode was not given (--locality or -l)");
                    return;
                }
                isRemote = locality.Equals("remote");

                p.AnalyzeAndLoad(solutionFolder, outputFolder, neo4jUrl, isRemote, justMyCodePattern, applicationsPattern);
            }
            else if (loadMode.Equals("analyze", StringComparison.OrdinalIgnoreCase))
            {
                p.Analyze(solutionFolder, outputFolder, justMyCodePattern, applicationsPattern);
            }
            else if (loadMode.Equals("load", StringComparison.OrdinalIgnoreCase))
            {
                string neo4jUrl = string.Empty;
                if (!argDict.TryGetValue("neo4j-url", out neo4jUrl))
                {
                    Console.WriteLine("The Neo4j url was not given (--neo4j-url or -n)");
                    return;
                }

                bool isRemote = false;
                string locality = string.Empty;
                if (!argDict.TryGetValue("locality", out locality))
                {
                    Console.WriteLine("The locality mode was not given (--locality or -l)");
                    return;
                }
                isRemote = locality.Equals("remote");

                p.BulkInsertOfCsvFolder(outputFolder, neo4jUrl, isRemote);
            }
            else
            {
                Console.WriteLine("Bad arguments");
            }
        }

        public void AnalyzeAndLoad(string solutionFolder, string rootCsvFolder, string neo4jUrl, bool isRemote, string companyAssembliesPattern, string applicationsPattern)
        {
            try
            {
                var codeService = BuildCodeService();
                var neoLoader = new NeoLoader();
                var methodGraphs = codeService.AnalyzeSolution(companyAssembliesPattern, applicationsPattern, solutionFolder);
                foreach (var methodGraph in methodGraphs)
                {
                    var csvFolder = neoLoader.GenerateLocalCsvFiles(methodGraph, rootCsvFolder);

                    var loadRequest = new LoadRequest()
                    {
                        Locality = isRemote ? Locality.Remote : Locality.Local,
                        Neo4jUrl = neo4jUrl
                    };
                    neoLoader.BulkLoadCsv(loadRequest, csvFolder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Analyze(string solutionFolder, string rootCsvFolder, string companyAssembliesPattern, string applicationsPattern)
        {
            try
            {
                var codeAnalyzer = BuildCodeService();
                var neoLoader = new NeoLoader();

                var methodGraphs = codeAnalyzer.AnalyzeSolution(companyAssembliesPattern, applicationsPattern, solutionFolder);
                foreach (var methodGraph in methodGraphs)
                {
                    var csvFolder = neoLoader.GenerateLocalCsvFiles(methodGraph, rootCsvFolder);
                    Console.WriteLine("Created: " + csvFolder);               
                }
            }
            catch (Exception ex)
            {
                // for debugging
                Console.WriteLine(ex.ToString());
            }
        }

        public void BulkInsertOfCsvFolder(string csvFolder, string neo4jUrl, bool isRemote)
        {
            try
            {
                var loadRequest = new LoadRequest()
                {
                    Locality = isRemote ? Locality.Remote : Locality.Local,
                    Neo4jUrl = neo4jUrl
                };
                var neoLoader = new NeoLoader();
                neoLoader.BulkLoadCsv(loadRequest, csvFolder);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private GraphodeCodeAnalyzer BuildCodeService()
        {
            var databaseAccessDetectors = new List<IDatabaseAccessDetector>();
            databaseAccessDetectors.Add(new AdoNetAccessDetector());
            databaseAccessDetectors.Add(new EfAccessDetector());

            return new GraphodeCodeAnalyzer(databaseAccessDetectors, new LocalAppConfigFinder());
        }
    }
}
