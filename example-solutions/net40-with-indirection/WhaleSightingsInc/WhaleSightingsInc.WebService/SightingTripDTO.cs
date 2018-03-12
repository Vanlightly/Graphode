using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WhaleSightingsInc.WebService
{
    [DataContract]
    public class SightingTripDTO
    {
        [DataMember]
        public List<WhaleSightingDTO> Sightings { get; set; }
        [DataMember]
        public DateTime DepartureTime { get; set; }
        [DataMember]
        public string Captain { get; set; }
    }
}
