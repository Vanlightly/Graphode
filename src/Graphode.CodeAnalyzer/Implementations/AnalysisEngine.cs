using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.CallGraph;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Graph;
using Graphode.CodeAnalyzer.Logging;

namespace Graphode.CodeAnalyzer.Implementations
{
    public class AnalysisEngine : IAnalysisEngine
    {
        private readonly IAssignmentGraphIndexer _assignmentGraphIndexer;
        private readonly IAssignmentGraphWalker _assignmentGraphWalker;
        private readonly IMethodIndexer _methodIndexer;
        private readonly IDelegateIndexer _delegateIndexer;
        private readonly ICallTreeWalker _callTreeWalker;
        private readonly ITypeService _typeService;
        private readonly ILogOutput _logOutput;

        private HashSet<string> _assembliesProcessed;
        private List<ModuleDefinition> _modulesToAnalyze;

        public AnalysisEngine(IAssignmentGraphIndexer assignmentGraphIndexer,
            IAssignmentGraphWalker assignmentGraphWalker,
            IMethodIndexer methodIndexer,
            IDelegateIndexer delegateIndexer,
            ICallTreeWalker callTreeWalker,
            ITypeService typeService,
            ILogOutput logOutput)
        {
            _assignmentGraphIndexer = assignmentGraphIndexer;
            _assignmentGraphWalker = assignmentGraphWalker;
            _methodIndexer = methodIndexer;
            _delegateIndexer = delegateIndexer;
            _callTreeWalker = callTreeWalker;
            _typeService = typeService;
            _logOutput = logOutput;
        }

        public AnalysisPhaseResult LoadApplication(string companyAssembliesPattern, ApplicationDetails application)
        {
            try
            {
                var assembliesFolder = AssemblyPathFinder.GetAssembliesFolder(application.FullFolderPath);
                if (assembliesFolder == null)
                    return AnalysisPhaseResult.CouldNotFindBinFolder;

                var companyAssembliesInFolder = AssemblyPathFinder.GetCompanyAssemblies(companyAssembliesPattern, assembliesFolder);
                if (!companyAssembliesInFolder.Any())
                    return AnalysisPhaseResult.NoCompanyDllsFound;

                AnalysisScope.ApplicationAssemblies = companyAssembliesInFolder;

                EmptyIndexes();
                foreach (var companyAssembly in companyAssembliesInFolder)
                {
                    var module = DecompilerService.GetModuleDefinition(assembliesFolder, companyAssembly);
                    _modulesToAnalyze.Add(module);
                }

                foreach (var module in _modulesToAnalyze)
                {
                    if (!_assembliesProcessed.Contains(module.Assembly.FullName))
                    {
                        _assembliesProcessed.Add(module.Assembly.FullName);

                        _assignmentGraphIndexer.IndexTriples(module);
                        _methodIndexer.Load(module);
                    }
                }

                _delegateIndexer.Load(_modulesToAnalyze);
                _methodIndexer.BuildMethodObjects(application.CsProjName);
                _typeService.Load(_modulesToAnalyze);

                return AnalysisPhaseResult.Success;
            }
            catch(Exception ex)
            {
                _logOutput.LogError("Failed analyzing application. ", ex);
                return AnalysisPhaseResult.Failed;
            }
        }

        public List<string> GetAssignmentSource(string startObjectKey,
            ISourceDetector nameSourceDetector, 
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns)
        {
            var sources = new List<string>();
            var results = _assignmentGraphWalker.PerformBacktrackingSearch(startObjectKey, nameSourceDetector, permittedToGoInstancePatterns, searchBaseConstructorPatterns);
            foreach(var result in results)
            {
                foreach (var node in result.FoundNodes)
                {
                    sources.Add(node.Triple.From.ObjectKey);
                }
            }

            return sources;
        }

        public List<string> GetAssignmentSource(Instruction instruction, 
            MethodDefinition methodDefinition,
            ISourceDetector nameSourceDetector, 
            List<GoToInstancePattern> permittedToGoInstancePatterns,
            List<string> searchBaseConstructorPatterns)
        {
            var sources = new List<string>();
            var results = _assignmentGraphWalker.PerformBacktrackingSearch(instruction, methodDefinition, nameSourceDetector, permittedToGoInstancePatterns, searchBaseConstructorPatterns);
            foreach (var result in results)
            {
                foreach (var node in result.FoundNodes)
                {
                    sources.Add(node.Triple.From.ObjectKey);
                }
            }

            return sources;
        }

        public List<MethodGraph> BuildMethodGraphs(string applicationName, string companyAssembliesPattern)
        {
            var callGraphs = new List<MethodGraph>();
            callGraphs.Add(_callTreeWalker.BuildCrossAssemblyGraph(applicationName, companyAssembliesPattern, _modulesToAnalyze));
            callGraphs.Add(_callTreeWalker.BuildPublicInnerAssemblyGraph(applicationName, companyAssembliesPattern, _modulesToAnalyze));
            callGraphs.Add(_callTreeWalker.BuildFullGraph(applicationName, companyAssembliesPattern, _modulesToAnalyze));

            return callGraphs;
        }

        private void EmptyIndexes()
        {
            _assembliesProcessed = new HashSet<string>();
            _modulesToAnalyze = new List<ModuleDefinition>();

            _assignmentGraphIndexer.CleanIndexes();
            _delegateIndexer.CleanIndexes();
            _methodIndexer.CleanIndexes();
            _typeService.CleanIndexes();
            ResolveService.CleanIndexes();
            PropertyService.CleanIndexes();
            SignatureKeyService.CleanIndexes();
        }

          
    }
}
