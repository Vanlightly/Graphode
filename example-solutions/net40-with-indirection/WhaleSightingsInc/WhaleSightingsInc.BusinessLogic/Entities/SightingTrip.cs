using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhaleSightingsInc.BusinessLogic.Entities
{
    public class SightingTrip
    {
        public int Id { get; set; }
        public List<WhaleSighting> WhaleSightings { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Captain { get; set; }
    }
}
