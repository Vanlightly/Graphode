using Mono.Cecil;
using System;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph;
using Graphode.CodeAnalyzer.Exceptions;
using System.Diagnostics;
using Graphode.CodeAnalyzer.Logging;

namespace Graphode.CodeAnalyzer.Implementations.AssignmentGraph
{
    public class AssignmentGraphIndexer : IAssignmentGraphIndexer
    {
        private readonly IInstructionParser _instructionParser;
        private readonly ITripleStore _tripleStore;
        private readonly ILogOutput _logOutput;

        public AssignmentGraphIndexer(IInstructionParser instructionParser,
            ITripleStore tripleStore,
            ILogOutput logOutput)
        {
            _instructionParser = instructionParser;
            _tripleStore = tripleStore;
            _logOutput = logOutput;
        }

        public void CleanIndexes()
        {
            _tripleStore.CleanIndexes();
        }

        public void IndexTriples(ModuleDefinition module)
        {
            int counter = 0;
            int total = module.Types.Count;
            foreach (var type in module.Types)
            {
                counter++;
                _logOutput.LogAnalysis(counter + " types of " + total);
                IndexTriples(type);
            }
        }

        private void IndexTriples(TypeDefinition typeDefinition)
        {
            if (typeDefinition.BaseType != null && typeDefinition.BaseType.FullName.Equals("System.Data.Objects.DataClasses.EntityObject"))
                return;

            if (typeDefinition.HasNestedTypes)
            {
                foreach (var nestedType in typeDefinition.NestedTypes)
                    IndexTriples(nestedType);
            }

            foreach (var method in typeDefinition.Methods)
            {
                try
                {
                    var triples = _instructionParser.ParseInstructions(method);

                    foreach (var triple in triples)
                        _tripleStore.Add(triple);
                }
                catch(ILParseException ex)
                {
                    // the IL parsing is still a bit flaky
                    _logOutput.LogAnalysis(ex.Message);
                    _logOutput.LogAnalysis("CONTINUING!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Trace.TraceError(ex.ToString());
                }
                catch(Exception ex)
                {
                    _logOutput.LogAnalysis(ex.Message);
                    _logOutput.LogAnalysis("CONTINUING!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Trace.TraceError(ex.ToString());
                }
            }
        }

    }
}
