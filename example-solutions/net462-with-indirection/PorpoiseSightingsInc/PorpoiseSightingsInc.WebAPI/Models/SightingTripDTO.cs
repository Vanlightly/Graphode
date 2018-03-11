using System;
using System.Collections.Generic;

namespace PorpoiseSightingsInc.WebAPI.Models
{
    public class SightingTripDTO
    {
        public List<PorpoiseSightingDTO> Sightings { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Captain { get; set; }
    }
}
