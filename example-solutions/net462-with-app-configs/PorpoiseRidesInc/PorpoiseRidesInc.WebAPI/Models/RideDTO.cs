using System;

namespace PorpoiseRidesInc.WebAPI.Models
{
    public class RideDTO
    {
        public PorpoiseDTO Porpoise { get; set; }
        public DateTime Time { get; set; }
        public string Rider { get; set; }
    }
}
