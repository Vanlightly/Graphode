using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Implementations;
using Graphode.CodeAnalyzer.Entities;
using Graphode.CodeAnalyzer.Graph;

namespace Graphode.CodeAnalyzer.Contracts
{
    public interface IAnalysisEngine
    {
        AnalysisPhaseResult LoadApplication(string companyAssembliesPattern, ApplicationDetails application);
        List<MethodGraph> BuildMethodGraphs(string applicationName, string companyAssembliesPattern);
    }
}
