using System;
using System.Collections.Generic;
using Graphode.CodeAnalyzer.Entities.AssignmentGraph;
using Graphode.CodeAnalyzer.Entities.CallGraph;
using Graphode.CodeAnalyzer.Contracts.AssignmentGraph.ResourceAccesses;

namespace Graphode.CodeAnalyzer.Plugins.Databases.Detectors
{
    public class AdoNetAccessDetector : IDatabaseAccessDetector
    {
        private List<TargetMethod> _targetMethods;
        private Dictionary<string, TargetMethodMatch> _isTargetDict;

        public AdoNetAccessDetector()
        {
            _targetMethods = new List<TargetMethod>();
            _targetMethods.Add(new TargetMethod() { Target = "System.Data.Common.DbCommand::ExecuteScalar", AccessMode = "ADO.NET,MsSQL" });
            _targetMethods.Add(new TargetMethod() { Target = "System.Data.Common.DbCommand::ExecuteNonQuery", AccessMode = "ADO.NET,MsSQL" });
            _targetMethods.Add(new TargetMethod() { Target = "System.Data.SqlClient.SqlCommand::ExecuteReader", AccessMode = "ADO.NET,MsSQL" });
            _targetMethods.Add(new TargetMethod() { Target = "System.Data.SqlClient.SqlBulkCopy::WriteToServer", AccessMode = "ADO.NET,MsSQL" });
            _targetMethods.Add(new TargetMethod() { Target = "MySql.Data.MySqlClient.MySqlCommand::ExecuteScalar()", AccessMode = "ADO.NET,MySQL" });
            _targetMethods.Add(new TargetMethod() { Target = "MySql.Data.MySqlClient.MySqlCommand::ExecuteNonQuery()", AccessMode = "ADO.NET,MySQL" });
            _targetMethods.Add(new TargetMethod() { Target = "MySql.Data.MySqlClient.MySqlCommand::ExecuteReader()", AccessMode = "ADO.NET,MySQL" });

            _isTargetDict = new Dictionary<string, TargetMethodMatch>();
        }

        public TargetMethodMatch IsDatabaseAccess(MethodCall method, MethodObject currentMethod)
        {
            TargetMethodMatch targetMethodMatch = null;
            if (_isTargetDict.TryGetValue(method.MethodCalled.FullName, out targetMethodMatch))
                return targetMethodMatch;

            foreach (var targetMethod in _targetMethods)
            {
                if (method.MethodCalled.FullName.IndexOf(targetMethod.Target) > -1)
                {
                    var match = GetMatch(targetMethod.AccessMode);
                    _isTargetDict.Add(method.MethodCalled.FullName, match);
                    return match;
                }
            }

            // if it is not a database access method, then cache that result return no match
            _isTargetDict.Add(method.MethodCalled.FullName, GetNoMatch());
            return GetNoMatch();
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
