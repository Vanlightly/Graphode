using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;
using Mono.Cecil;
using Graphode.CodeAnalyzer.Implementations.Common;

namespace Graphode.CodeAnalyzer.Plugins.Databases.Detectors
{
    public class EfAccessDetector : IDatabaseAccessDetector
    {
        private HashSet<string> _isEntityFrameworkDict;

        public EfAccessDetector()
        {
            _isEntityFrameworkDict = new HashSet<string>();
        }

        public TargetMethodMatch IsDatabaseAccess(MethodCall method, MethodObject currentMethod)
        {
            if (_isEntityFrameworkDict.Contains(currentMethod.GetMethodDefinition().FullName))
                return GetMatch("EntityFramework");

            if (UsesEntityFramework(currentMethod))
            {
                _isEntityFrameworkDict.Add(currentMethod.GetMethodDefinition().FullName);
                return GetMatch("EntityFramework");
            }

            return GetNoMatch();
        }

        private bool UsesEntityFramework(MethodObject currentMethod)
        { 
            bool usesEfField = false;
            bool usesEfMethod = false;
            bool classInheritsFromObjectContext = false;

            if (currentMethod.HasImplementation())
            {
                usesEfField = currentMethod.FieldsRead.Any(
                        t => t.DeclaringType.Namespace.Equals("System.Data.EntityClient")
                             || t.DeclaringType.Namespace.Equals("System.Data.Objects"));

                if (!usesEfField)
                {
                    var declaringTypes = currentMethod.MethodsCalled
                            .Select(t => t.MethodCalled.DeclaringType)
                            .GroupBy(x => x.FullName)
                            .Select(x => x.First())
                            .ToList();

                    foreach(var declaringType in declaringTypes)
                    {
                        if (declaringType.Namespace.Equals("System.Data.EntityClient")
                             || declaringType.Namespace.Equals("System.Data.Objects")
                             || declaringType.Namespace.Equals("System.Data.Entity"))
                        {
                            usesEfMethod = true;
                            break;
                        }

                        TypeDefinition typeDef = null;
                        if(ResolveService.TryResolve(declaringType, out typeDef))
                        {
                            if(typeDef.BaseType != null && (typeDef.BaseType.Namespace.Equals("System.Data.EntityClient")
                             || typeDef.BaseType.Namespace.Equals("System.Data.Objects")
                             || typeDef.BaseType.Namespace.Equals("System.Data.Entity")))
                            {
                                usesEfMethod = true;
                                break;
                            }
                        }
                    }
                }
            }

            return usesEfField || usesEfMethod || classInheritsFromObjectContext;
        }

        private TargetMethodMatch GetMatch(string accessMode)
        {
            return new TargetMethodMatch()
            {
                IsMatch = true,
                AccessMode = accessMode
            };
        }

        private TargetMethodMatch GetNoMatch()
        {
            return new TargetMethodMatch()
            {
                IsMatch = false
            };
        }
    }
}
