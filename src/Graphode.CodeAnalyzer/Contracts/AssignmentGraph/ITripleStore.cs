using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Contracts.Common;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;

namespace Graphode.CodeAnalyzer.Contracts.AssignmentGraph
{
    public interface ITripleStore : ICleanableIndex
    {
        void Add(Triple triple);
        List<Triple> GetAllTriples();
        List<Triple> Next(Triple triple);
        //List<Triple> NextByLinkKey(Triple triple);
        List<Triple> GetFrom(string objectKey);
        List<Triple> Back(Triple triple);
        //List<Triple> BackViaLinkKey(Triple triple);
        List<Triple> GetTo(string objectKey);
        List<Triple> GetToViaInstanceOwnerKey(string instanceOwnerKey);

        List<Triple> GetFromViaInstructionKey(string instructionKey);
        List<Triple> GetToViaInstructionKey(string instructionKey);
        List<Triple> GetToViaContructorInstructionKey(string constructorInstructionKey);
    }
}
