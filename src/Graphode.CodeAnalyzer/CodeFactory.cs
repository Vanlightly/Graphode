using Graphode.CodeAnalyzer.Contracts;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;
using Graphode.CodeAnalyzer.Implementations;
using Graphode.CodeAnalyzer.Implementations.AssignmentGraph;
using Graphode.CodeAnalyzer.Implementations.CallGraph;
using Graphode.CodeAnalyzer.Implementations.Common;
using Graphode.CodeAnalyzer.Implementations.Resolvers;
using Graphode.CodeAnalyzer.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer
{
    internal class CodeFactory
    {
        public static IAnalysisEngine BuildAnalysisEngine(List<IDatabaseAccessDetector> dbAccessDetectors, IDatabaseFinder databaseFinder)
        {
            var logOutput = new LogOutput();

            // single instance services
            var typeService = new TypeService();
            var tripleStore = new TripleStore(typeService);
            var assignmentGraphWalker = new AssignmentGraphWalker(tripleStore);
            var delegateIndexer = new DelegateIndexer(assignmentGraphWalker);
            var methodIndexer = new MethodIndexer(delegateIndexer);

            var databaseResolver = new DatabaseResolver(assignmentGraphWalker, new AnalysisLogger(), dbAccessDetectors, databaseFinder);

            return new AnalysisEngine(
                    new AssignmentGraphIndexer(new InstructionParser(logOutput), tripleStore, logOutput),
                    assignmentGraphWalker, methodIndexer, delegateIndexer,
                    new CallTreeWalker(methodIndexer, databaseResolver, logOutput),
                    typeService, logOutput);
        }

        public static ILogOutput BuildLogOutput()
        {
            return new LogOutput();
        }
    }
}
