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
        MethodGraph BuildMethodGraph(string applicationName, string companyAssembliesPattern);
    }
}
