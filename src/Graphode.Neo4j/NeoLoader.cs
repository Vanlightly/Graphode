using Neo4jClient;
using Neo4jClient.Cypher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Graphode.Neo4j.FileTransfer;
using Graphode.Neo4j.Helpers;
using Graphode.CodeAnalyzer.Graph;
using Graphode.CodeAnalyzer.Entities.CallGraph;

namespace Graphode.Neo4j
{
    public class NeoLoader
    {
        private readonly IFtpService _ftpService;

        public NeoLoader(IFtpService ftpService = null)
        {
            if (ftpService == null)
                _ftpService = new FtpService();
            else
                _ftpService = ftpService;
        }

        public string GenerateLocalCsvFiles(MethodGraph methodGraph, string rootCsvFolder)
        {
            var csvFolder = PrepareCsvFolder(rootCsvFolder, methodGraph.ApplicationName, methodGraph.GraphType);
            GenerateMethodNodesCsv(methodGraph.GetMethodNodes(), csvFolder, methodGraph.ApplicationName);
            GenerateResourceAccessesCsv(methodGraph.GetResourceAccessNodes(), csvFolder, methodGraph.ApplicationName);
            GenerateMethodToMethodRelationshipsCsv(methodGraph.GenerateMethodRelationships(), csvFolder, methodGraph.ApplicationName);
            GenerateMethodToResourceRelationshipsCsv(methodGraph.GenerateResourceRelationships(), csvFolder, methodGraph.ApplicationName);

            return csvFolder;
        }

        public void BulkLoadCsv(LoadRequest loadRequest, string csvFolderPath)
        {
            var folder = new DirectoryInfo(csvFolderPath);
            if (!folder.GetFiles().Any(x => x.Name.EndsWith("csv")))
            {
                return;
            }

            DeleteApplication(loadRequest, loadRequest.ApplicationName, loadRequest.GraphType);
            LoadMethodNodes(loadRequest, csvFolderPath);
            LoadResourceAccesses(loadRequest, csvFolderPath);
            LoadMethodToMethodRelationships(loadRequest, csvFolderPath);
            LoadMethodToResourceRelationships(loadRequest, csvFolderPath);
        }

        public void DeleteApplication(LoadRequest loadRequest, string applicationName, string graphType)
        {
            var query = "MATCH (n { app_domain: '" + applicationName + "', graph_type: '" + graphType + "' })-[r]-() DELETE r";
            ExecuteQuery(loadRequest.Neo4jUrl, query);
            query = "MATCH (n { app_domain: '" + applicationName + "', graph_type: '" + graphType + "' }) DELETE n";
            ExecuteQuery(loadRequest.Neo4jUrl, query);
        }

        public string PrepareCsvFolder(string rootCsvFolder, string appDomain, GraphType graphType)
        {
            string folder = Path.Combine(rootCsvFolder, appDomain, graphType.ToString(), Guid.NewGuid().ToString());
            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder))
                    File.Delete(file);
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        #region Method Nodes 

        public void GenerateMethodNodesCsv(List<MethodNode> methodNodes, string rootCsvFolder, string appDomain)
        {
            bool moreLeft = true;
            int batchNo = 1;

            while (moreLeft)
            {
                var batch = methodNodes.Skip((batchNo - 1) * 1000).Take(1000).ToList();
                if (batch.Any())
                {
                    CreateCsv(batch, batchNo, rootCsvFolder, appDomain);
                }
                else
                {
                    moreLeft = false;
                }

                batchNo++;
            }
        }

        public void LoadMethodNodes(LoadRequest loadRequest, string csvFileFolderPath)
        {
            var folder = new DirectoryInfo(csvFileFolderPath);
            var methodNodeFiles = folder.GetFiles().Where(x => x.Name.StartsWith("methods")).ToList();
            foreach (var methodNodeFile in methodNodeFiles)
            {
                string neo4jCsvfileName = methodNodeFile.FullName;
                if(loadRequest.Locality == Locality.Remote)
                    neo4jCsvfileName = SendFileToRemoteNeo4j(loadRequest, methodNodeFile.FullName);
                ExecuteMethodNodeQuery(loadRequest.Neo4jUrl, neo4jCsvfileName);
            }

            string fromIndexQuery = "CREATE INDEX ON :Method(from_id)";
            ExecuteQuery(loadRequest.Neo4jUrl, fromIndexQuery);

            string toIndexQuery = "CREATE INDEX ON :Method(to_id)";
            ExecuteQuery(loadRequest.Neo4jUrl, toIndexQuery);
        }

        private void CreateCsv(List<MethodNode> methodNodes, int batchNo, string rootCsvFolder, string appDomain)
        {
            var csvLines = new List<string>();
            csvLines.Add("FromId,ToId,GraphType,Method,AppDomain,ConcreteAssembly,ConcreteAssemblyVersion,ConcreteType,InterfaceAssembly,InterfaceAssemblyVersion,InterfaceType,"
                + "AbstractAssembly,AbstractAssemblyVersion,AbstractType,BaseClassAssembly,BaseClassAssemblyVersion,BaseClassType,AssemblyFriendly,MethodFriendly");

            foreach (var method in methodNodes)
            {
                var line = CsvWriter.Escape(method.GetFromNodeId())
                            + "," + CsvWriter.Escape(method.GetToNodeId())
                            + "," + CsvWriter.Escape(method.GraphType.ToString())
                            + "," + CsvWriter.Escape(method.MethodName)
                            + "," + method.AppDomain;

                if (method.ConcreteType == null)
                {
                    line += ",,,";
                }
                else
                {
                    line += "," + CsvWriter.Escape(method.ConcreteType.AssemblyName)
                                + "," + CsvWriter.Escape(method.ConcreteType.AssemblyVersion)
                                + "," + CsvWriter.Escape(method.ConcreteType.TypeName);
                }

                if (method.InterfaceType == null)
                {
                    line += ",,,";
                }
                else
                {
                    line += "," + CsvWriter.Escape(method.InterfaceType.AssemblyName)
                                + "," + CsvWriter.Escape(method.InterfaceType.AssemblyVersion)
                                + "," + CsvWriter.Escape(method.InterfaceType.TypeName);
                }

                if (method.AbstractType == null)
                {
                    line += ",,,";
                }
                else
                {
                    line += "," + CsvWriter.Escape(method.AbstractType.AssemblyName)
                                + "," + CsvWriter.Escape(method.AbstractType.AssemblyVersion)
                                + "," + CsvWriter.Escape(method.AbstractType.TypeName);
                }

                if (method.BaseClassType == null)
                {
                    line += ",,,";
                }
                else
                {
                    line += "," + CsvWriter.Escape(method.BaseClassType.AssemblyName)
                                + "," + CsvWriter.Escape(method.BaseClassType.AssemblyVersion)
                                + "," + CsvWriter.Escape(method.BaseClassType.TypeName);
                }

                // friendly assembly name
                string assemblyFriendly = string.Empty;
                string typeFriendly = string.Empty;
                if (method.ConcreteType != null)
                {
                    assemblyFriendly = CsvWriter.Escape(method.ConcreteType.AssemblyName).Replace(".", " ");
                    typeFriendly = ConvertToViewName(CsvWriter.Escape(method.ConcreteType.TypeName.Substring(method.ConcreteType.TypeName.LastIndexOf(".") + 1)));
                }
                else if (method.InterfaceType != null)
                {
                    assemblyFriendly = CsvWriter.Escape(method.InterfaceType.AssemblyName).Replace(".", " ");
                    typeFriendly = ConvertToViewName(CsvWriter.Escape(method.InterfaceType.TypeName.Substring(method.InterfaceType.TypeName.LastIndexOf(".") + 1)));
                }


                var methodFriendly = ConvertToViewName(CsvWriter.Escape(method.MethodName.Substring(0, method.MethodName.IndexOf("("))));
                var friendly = methodFriendly + " / " + typeFriendly;
                line += "," + assemblyFriendly + "," + friendly;

                csvLines.Add(line);
            }

            SaveFile(rootCsvFolder, "methods" + batchNo + ".csv", csvLines);
        }

        private void ExecuteMethodNodeQuery(string neo4jUrl, string methodNodeFile)
        {
            var csvFilePathNeoStyle = methodNodeFile.Replace("\\", "/");

            var query = string.Format(
@"LOAD CSV WITH HEADERS FROM ""file:///{0}"" AS csvLine
CREATE (a:{1} {{ from_id: csvLine.FromId, to_id: csvLine.ToId, graph_type: csvLine.GraphType, method: csvLine.Method, app_domain: csvLine.AppDomain,
concrete_assembly: csvLine.ConcreteAssembly, concrete_assembly_version: csvLine.ConcreteAssemblyVersion, concrete_type: csvLine.ConcreteType, 
interface_assembly: csvLine.InterfaceAssembly, interface_assembly_version: csvLine.InterfaceAssemblyVersion, interface_type: csvLine.InterfaceType,
abstract_assembly: csvLine.AbstractAssembly, abstract_assembly_version: csvLine.AbstractAssemblyVersion, abstract_type: csvLine.AbstractType,
base_class_assembly: csvLine.BaseClassAssembly, base_class_assembly_version: csvLine.BaseClassAssemblyVersion, base_class_type: csvLine.BaseClassType, 
assembly_friendly: csvLine.AssemblyFriendly, method_friendly: csvLine.MethodFriendly, friendly: csvLine.Friendly }})"
, csvFilePathNeoStyle, "Method");

            ExecuteQuery(neo4jUrl, query);
        }

        #endregion Method Nodes 


        #region Resource Access Nodes 

        public void GenerateResourceAccessesCsv(List<ResourceAccessNode> resourceAccessNodes, string rootCsvFolder, string appDomain)
        {
            bool moreLeft = true;
            int batchNo = 1;

            while (moreLeft)
            {
                var batch = resourceAccessNodes.Skip((batchNo - 1) * 1000).Take(1000).ToList();
                if (batch.Any())
                {
                    CreateCsv(batch, batchNo, rootCsvFolder, appDomain);
                }
                else
                {
                    moreLeft = false;
                }

                batchNo++;
            }
        }

        public void LoadResourceAccesses(LoadRequest loadRequest, string csvFileFolderPath)
        {
            var folder = new DirectoryInfo(csvFileFolderPath);
            var csvFiles = folder.GetFiles().Where(x => x.Name.StartsWith("resources")).ToList();
            foreach (var csvFile in csvFiles)
            {
                string fileName = csvFile.FullName;
                if(loadRequest.Locality == Locality.Remote)
                    fileName = SendFileToRemoteNeo4j(loadRequest, csvFile.FullName);
                ExecuteResourceAccessNodeQuery(loadRequest.Neo4jUrl, fileName);
            }

            string indexQuery = "CREATE INDEX ON :ResourceAccess(id)";
            ExecuteQuery(loadRequest.Neo4jUrl, indexQuery);
        }

        private void CreateCsv(List<ResourceAccessNode> resourceNodes, int batchNo, string rootCsvFolder, string appDomain)
        {
            var csvLines = new List<string>();
            csvLines.Add("Id,ResourceType,GraphType,ResourceName,AppDomain,AssignmentApproach");

            foreach (var resource in resourceNodes)
            {
                csvLines.Add(resource.GetNodeId()
                    + "," + CsvWriter.Escape(resource.ConfigurationResource.ToString())
                    + "," + CsvWriter.Escape(resource.GraphType.ToString())
                    + "," + CsvWriter.Escape(resource.ResourceKey.Value)
                    + "," + resource.AppDomain
                    + "," + CsvWriter.Escape(resource.ResourceKey.Approach.ToString()));
            }

            SaveFile(rootCsvFolder, "resources" + batchNo + ".csv", csvLines);
        }

        private void ExecuteResourceAccessNodeQuery(string neo4jUrl, string csvFilePath)
        {
            var csvFilePathNeoStyle = csvFilePath.Replace("\\", "/");

            var query = string.Format(
@"LOAD CSV WITH HEADERS FROM ""file:///{0}"" AS csvLine
CREATE (a:{1} {{ id: csvLine.Id, type: csvLine.ResourceType, graph_type: csvLine.GraphType, name: csvLine.ResourceName, app_domain: csvLine.AppDomain, assignment_approach: csvLine.AssignmentApproach }})"
, csvFilePathNeoStyle, "ResourceAccess");

            ExecuteQuery(neo4jUrl, query);
        }

        #endregion Resource Access Nodes 


        #region Method to Method Relationships

        public void GenerateMethodToMethodRelationshipsCsv(List<MethodToMethodRelationship> methodToMethodRelationships, string rootCsvFolder, string appDomain)
        {
            bool moreLeft = true;
            int batchNo = 1;

            while (moreLeft)
            {
                var batch = methodToMethodRelationships.Skip((batchNo - 1) * 1000).Take(1000).ToList();
                if (batch.Any())
                {
                    CreateCsv(batch, batchNo, rootCsvFolder, appDomain);
                }
                else
                {
                    moreLeft = false;
                }

                batchNo++;
            }
        }

        public void LoadMethodToMethodRelationships(LoadRequest loadRequest, string csvFileFolderPath)
        {
            var folder = new DirectoryInfo(csvFileFolderPath);
            var csvFiles = folder.GetFiles().Where(x => x.Name.StartsWith("method_relationships")).ToList();
            foreach (var csvFile in csvFiles)
            {
                string fileName = csvFile.FullName;
                if(loadRequest.Locality == Locality.Remote)
                    fileName = SendFileToRemoteNeo4j(loadRequest, csvFile.FullName);
                ExecuteMethodToMethodQuery(loadRequest.Neo4jUrl, fileName);
            }
        }

        private void CreateCsv(List<MethodToMethodRelationship> methodToMethodRelationships, int batchNo, string rootCsvFolder, string appDomain)
        {
            var csvLines = new List<string>();
            csvLines.Add("CallerId,CalleeId");

            foreach (var method in methodToMethodRelationships)
            {
                csvLines.Add(CsvWriter.Escape(method.Caller.GetFromNodeId()) + "," + CsvWriter.Escape(method.Callee.GetToNodeId()));
            }

            SaveFile(rootCsvFolder, "method_relationships" + batchNo + ".csv", csvLines);
        }

        private void ExecuteMethodToMethodQuery(string neo4jUrl, string csvFilePath)
        {
            var csvFilePathNeoStyle = csvFilePath.Replace("\\", "/");

            string query = string.Format(
@"USING PERIODIC COMMIT 
LOAD CSV WITH HEADERS FROM ""file:///{0}"" AS csvLine 
MATCH (from:{1} {{ from_id: csvLine.{2}}}),(to:{3} {{ to_id: csvLine.{4}}})
CREATE (from)-[:{5}]->(to)"
, csvFilePathNeoStyle, "Method", "CallerId", "Method", "CalleeId", "CALLS");

            ExecuteQuery(neo4jUrl, query);
        }

        #endregion Method to Method Relationships


        #region Method to Resource Relationships

        public void GenerateMethodToResourceRelationshipsCsv(List<MethodToResourceRelationship> methodToResourceRelationships, string rootCsvFolder, string appDomain)
        {
            bool moreLeft = true;
            int batchNo = 1;

            while (moreLeft)
            {
                var batch = methodToResourceRelationships.Skip((batchNo - 1) * 1000).Take(1000).ToList();
                if (batch.Any())
                {
                    CreateCsv(batch, batchNo, rootCsvFolder, appDomain);
                }
                else
                {
                    moreLeft = false;
                }

                batchNo++;
            }
        }

        public void LoadMethodToResourceRelationships(LoadRequest loadRequest, string csvFileFolderPath)
        {
            var folder = new DirectoryInfo(csvFileFolderPath);
            var csvFiles = folder.GetFiles().Where(x => x.Name.StartsWith("resource_relationships")).ToList();
            foreach (var csvFile in csvFiles)
            {
                string neo4jCsvFileName = csvFile.FullName;
                if(loadRequest.Locality == Locality.Remote)
                    neo4jCsvFileName = SendFileToRemoteNeo4j(loadRequest, csvFile.FullName);
                ExecuteMethodToResourceQuery(loadRequest.Neo4jUrl, neo4jCsvFileName);
            }
        }

        private void CreateCsv(List<MethodToResourceRelationship> resourceRelationships, int batchNo, string rootCsvFolder, string appDomain)
        {
            var csvLines = new List<string>();
            csvLines.Add("CallerId,ResourceId,MatchAlgorithm,AccessMode");

            foreach (var resourceRelationship in resourceRelationships)
            {
                csvLines.Add(CsvWriter.Escape(resourceRelationship.Caller.GetFromNodeId())
                    + "," + resourceRelationship.Resource.GetNodeId()
                    + "," + resourceRelationship.Resource.ResourceKey.Approach
                    + "," + resourceRelationship.Resource.ResourceKey.AccessMode);
            }

            SaveFile(rootCsvFolder, "resource_relationships" + batchNo + ".csv", csvLines);
        }

        private void ExecuteMethodToResourceQuery(string neo4jUrl, string csvFilePath)
        {
            var csvFilePathNeoStyle = csvFilePath.Replace("\\", "/");

            string query = string.Format(
@"USING PERIODIC COMMIT 
LOAD CSV WITH HEADERS FROM ""file:///{0}"" AS csvLine 
MATCH (from:{1} {{ from_id: csvLine.{2}}}),(to:{3} {{ id: csvLine.{4}}})
CREATE (from)-[:{5} {{ match_algorithm: csvLine.MatchAlgorithm, access_mode: csvLine.AccessMode }}]->(to)"
, csvFilePathNeoStyle, "Method", "CallerId", "ResourceAccess", "ResourceId", "ACCESSES");

            ExecuteQuery(neo4jUrl, query);
        }

        #endregion Method to Resource Relationships


        private void ExecuteQuery(string neo4jUrl, string query)
        {
            var graphClient = new GraphClient(new Uri(neo4jUrl));
            graphClient.Connect();
            var graphQuery = new CypherQuery(query, new Dictionary<string, object>(), CypherResultMode.Projection, null);
            ((IRawGraphClient)graphClient).ExecuteCypher(graphQuery);
        }

        private void SaveFile(string rootCsvFolder, string filename, List<string> csvLines)
        {
            var localFilePath = Path.Combine(rootCsvFolder, filename);
            File.WriteAllLines(localFilePath, csvLines);
        }

        private string SendFileToRemoteNeo4j(LoadRequest loadRequest, string fullFilePath)
        {
            string path = string.Empty;
            int tryCount = 0;
            bool loaded = false;
            while (loaded == false && tryCount < 3)
            {
                tryCount++;

                try
                {
                    var randomizedFileName = Guid.NewGuid().ToString() + ".csv";
                    var ftpPath = "CSV/" + randomizedFileName;
                    _ftpService.UploadFile(loadRequest.FtpBaseUrl, ftpPath, File.ReadAllText(fullFilePath));

                    path = loadRequest.NeoCsvFolder + "\\" + randomizedFileName;
                    
                    loaded = true;
                }
                catch (Exception)
                {
                    if (tryCount == 3)
                        throw;
                }
            }

            return path;
        }

        public string ConvertToViewName(string name)
        {
            return Regex.Replace(name, "([a-z])_?([A-Z])", "$1 $2").Replace('.', ' ');
        }
    }
}
