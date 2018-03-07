using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WhaleRidesInc.WebService
{
    [DataContract]
    public class WhaleDTO
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Species { get; set; }
    }
}
