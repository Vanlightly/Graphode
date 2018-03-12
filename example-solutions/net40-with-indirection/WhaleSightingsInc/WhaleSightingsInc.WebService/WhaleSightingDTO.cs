using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WhaleSightingsInc.WebService
{
    [DataContract]
    public class WhaleSightingDTO
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Species { get; set; }

        [DataMember]
        public decimal Longitude { get; set; }

        [DataMember]
        public decimal Latitude { get; set; }

        [DataMember]
        public DateTime Time { get; set; }
    }
}
