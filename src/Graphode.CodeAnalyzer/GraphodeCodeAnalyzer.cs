using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Graphode.CodeAnalyzer.Contracts;
using Graphode.CodeAnalyzer.Implementations;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;
using Graphode.CodeAnalyzer.Code;
using Graphode.CodeAnalyzer.Graph;
using Graphode.CodeAnalyzer.Logging;

namespace Graphode.CodeAnalyzer
{
    public class GraphodeCodeAnalyzer
    {
        private readonly IAnalysisEngine _analysisEngine;
        private readonly ILogOutput _logOutput;

        public GraphodeCodeAnalyzer(List<IDatabaseAccessDetector> dbAccessDetectors, 
            IDatabaseFinder databaseFinder,
            IAnalysisEngine analysisEngine = null,
            ILogOutput logOutput = null)
        {
            if (analysisEngine == null)
                _analysisEngine = CodeFactory.BuildAnalysisEngine(dbAccessDetectors, databaseFinder);
            else
                _analysisEngine = analysisEngine;

            if (logOutput == null)
                _logOutput = CodeFactory.BuildLogOutput();
            else
                _logOutput = logOutput;
        }

        public List<MethodGraph> AnalyzeSolution(string companyAssembliesPattern, string applicationsPattern, string solutionFolder)
        {
            try
            {
                _logOutput.LogInfo(" ");
                _logOutput.LogInfo("================================================");
                _logOutput.LogInfo("SOLUTION ANALISIS STARTED: " + solutionFolder);

                var solution = GetSolution(solutionFolder, applicationsPattern);
                if(!solution.Applications.Any())
                {
                    _logOutput.LogInfo("No applications found in solution. Process aborted.");
                    return new List<MethodGraph>();
                }

                AnalysisScope.CurrentSolutionPath = solutionFolder;
                AnalysisScope.CurrentSolutionName = solution.Name;

                var methodGraphs = new List<MethodGraph>();
                foreach (var application in solution.Applications)
                {
                    var methodGraphsOfApp = AnalyzeApplication(companyAssembliesPattern, application);
                    methodGraphs.AddRange(methodGraphsOfApp);
                }

                return methodGraphs;
            }
            catch (Exception ex)
            {
                _logOutput.LogError("Failed to process solution " + ex);
                return new List<MethodGraph>();
            }
        }

        private VsSolution GetSolution(string solutionFolderPath, string applicationsPattern)
        {
            var sln = new VsSolution();
            var solutionFolder = new DirectoryInfo(solutionFolderPath);
            var solutionFiles = solutionFolder.GetFiles().Where(x => x.Extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)).ToList();
            FileInfo solutionFile = null;

            if (solutionFiles.Count == 0)
            {
                return sln;
            }
            else if (solutionFiles.Count == 1)
            {
                solutionFile = solutionFiles.First();
            }
            else
            {
                var found = false;
                foreach (var file in solutionFiles)
                {
                    if (solutionFolder.Name.IndexOf(file.Name.Replace(".sln", "")) > -1)
                    {
                        solutionFile = file;
                        found = true;
                        break;
                    }
                }

                if (!found)
                    solutionFile = solutionFiles.First();
            }

            var applicationDetails = GetApplicationDetails(solutionFolderPath, solutionFile.FullName, applicationsPattern);
            
            sln.Name = solutionFile.Name.Replace(".sln", "");
            sln.FolderName = solutionFolder.Parent.Name;
            sln.Applications = applicationDetails;

            return sln;
        }

        private List<ApplicationDetails> GetApplicationDetails(string solutionFolder, string solutionFilePath, string applicationsPattern)
        {
            var applicationFolders = AssemblyPathFinder.GetApplicationFolders(solutionFolder, applicationsPattern);

            var fileLines = File.ReadAllLines(solutionFilePath);
            var declaredApplications = new List<ApplicationDetails>();

            foreach (var line in fileLines.Where(x => x.StartsWith("Project") && x.IndexOf("csproj") > -1))
            {
                var afterEquals = line.Substring(line.IndexOf("=") + 1);

                int counter = 0;
                int startIndex = 0;
                int endIndex = 0;
                for (int i = 0; i < afterEquals.Length; i++)
                {
                    var letter = afterEquals[i];
                    if (letter == '"')
                    {
                        counter++;

                        if (counter == 3)
                            startIndex = i + 1;
                        else if (counter == 4)
                            endIndex = i - 1;
                    }
                }

                var targetText = afterEquals.Substring(startIndex, endIndex - startIndex + 1);
                int slashIndex = targetText.LastIndexOf("\\");
                if (slashIndex > -1)
                {
                    var folderName = targetText.Substring(0, slashIndex);
                    if (folderName.IndexOf("\\") > -1)
                        folderName = folderName.Substring(folderName.LastIndexOf("\\"));

                    var projectName = targetText.Substring(slashIndex + 1).Replace(".csproj", "");
                    var appFolderMatch = applicationFolders.SingleOrDefault(x => x.Contains(folderName) && x.Contains(projectName));
                    if (!string.IsNullOrEmpty(appFolderMatch))
                    {
                        var appDetails = new ApplicationDetails();
                        appDetails.CsProjName = projectName;
                        appDetails.FolderName = folderName;
                        appDetails.FullFolderPath = appFolderMatch;

                        declaredApplications.Add(appDetails);
                    }
                    else
                    {
                        // for debugging
                    }
                }
            }

            var deduplicatedFullList = declaredApplications.GroupBy(x => x.CsProjName).Select(x => x.First()).ToList();

            return deduplicatedFullList;
        }

        private List<MethodGraph> AnalyzeApplication(string companyAssembliesPattern, ApplicationDetails application)
        {
            try
            {
                _logOutput.LogInfo("----------------------------------");
                _logOutput.LogInfo("Analysis started for application: " + application.CsProjName);
                AnalysisScope.CurrentApplicationName = application.CsProjName;
                AnalysisScope.CurrentApplicationPath = application.FullFolderPath;
                
                AnalysisPhaseResult loaded = _analysisEngine.LoadApplication(companyAssembliesPattern, application);
                if (loaded != AnalysisPhaseResult.Success)
                {
                    _logOutput.LogError("Unable to analyse application. Processing aborted. Reason = " + loaded);
                    _logOutput.ApplicationAnalisisComplete(application.CsProjName, loaded.ToString());
                    return new List<MethodGraph>();
                }

                _logOutput.LogInfo("Code indexes built");

                return _analysisEngine.BuildMethodGraphs(application.CsProjName, companyAssembliesPattern);
            }
            catch (Exception ex)
            {
                _logOutput.LogError("Processing failed", ex);
                _logOutput.ApplicationAnalisisComplete(application.CsProjName, "Failed");
                return new List<MethodGraph>();
            }
        }
    }
}
