using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Entities.Common;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;

namespace Graphode.CodeAnalyzer.Implementations.Resolvers
{
    /// <summary>
    /// Responsible for:
    /// 1. Detecting when an invoked method is a database access
    /// 2. Using various strategies to *try* to find which database the method accesses
    /// </summary>
    public class DatabaseResolver : IDatabaseResolver
    {
        private readonly IAssignmentGraphWalker _assignmentGraphWalker;
        private readonly IAnalysisLogger _analysisLogger;
        private readonly List<IDatabaseAccessDetector> _databaseAccessDetectors;
        private readonly IDatabaseFinder _databaseFinder;

        public DatabaseResolver(IAssignmentGraphWalker assignmentGraphWalker,
            IAnalysisLogger analysisLogger,
            List<IDatabaseAccessDetector> databaseAccessDetectors,
            IDatabaseFinder databaseFinder)
        {
            _assignmentGraphWalker = assignmentGraphWalker;
            _analysisLogger = analysisLogger;
            _databaseAccessDetectors = databaseAccessDetectors;
            _databaseFinder = databaseFinder;
        }

        /// <summary>
        /// Returns an object that says if the method called is a database access or not
        /// </summary>
        /// <param name="method">The method that is invoked, the focus of this check</param>
        /// <param name="currentMethod">The method that called it</param>
        /// <returns>A TargetMethodMatch which indicates if the method called is a database access or not</returns>
        public TargetMethodMatch IsTargetMethodMatch(MethodCall method, MethodObject currentMethod)
        {
            foreach(var detector in _databaseAccessDetectors)
            {
                var result = detector.IsDatabaseAccess(method, currentMethod);
                if (result.IsMatch)
                    return result;
            }

            return new TargetMethodMatch() { IsMatch = false };
        }
               

        /// <summary>
        /// Returns an object that contains the name of the database or an alias
        /// </summary>
        /// <param name="targetMethodMatch">The object returned by the IsTargetMethodMatch method which indicates what type of db access it is</param>
        /// <param name="methodCall">The method that is invoked, the focus of this check</param>
        /// <param name="currentMethod">The method that called it</param>
        /// <returns></returns>
        public ResourceKeyResult GetDatabaseKey(TargetMethodMatch targetMethodMatch, MethodCall methodCall, MethodObject currentMethod)
        {
            var result = _databaseFinder.FindDatabaseName(targetMethodMatch, methodCall, currentMethod, _assignmentGraphWalker);
            return new ResourceKeyResult()
            {
                AccessMode = targetMethodMatch.AccessMode,
                Value = result
            };
        }
    }
}
