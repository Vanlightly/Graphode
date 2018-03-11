using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PorpoiseSightingsInc.BusinessLogic.Entities
{
    public class SightingTrip
    {
        public int Id { get; set; }
        public List<PorpoiseSighting> PorpoiseSightings { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Captain { get; set; }
    }
}
