using System;

namespace PorpoiseSightingsInc.WebAPI.Models
{
    public class PorpoiseSightingDTO
    {
        public string Name { get; set; }
        public string Species { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public DateTime Time { get; set; }
    }
}
