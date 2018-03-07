using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WhaleRidesInc.WebService
{
    [DataContract]
    public class RideDTO
    {
        [DataMember]
        public WhaleDTO Whale { get; set; }

        [DataMember]
        public DateTime Time { get; set; }
        [DataMember]
        public string Rider { get; set; }
    }
}
