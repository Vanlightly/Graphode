using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Graphode.CodeAnalyzer.Exceptions
{
    [Serializable]
    public class ConfigurationInitializationException : Exception
    {

        public ConfigurationInitializationException()
        {

        }
        public ConfigurationInitializationException(string message) : base(message)
        {

        }
        public ConfigurationInitializationException(string message, Exception innerException) : base(message, innerException)
        {

        }
        protected ConfigurationInitializationException(SerializationInfo info,
         StreamingContext context)
            : base(info, context)
        {

        }

    }
}
