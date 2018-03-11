using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PorpoiseSightingsInc.BusinessLogic.Entities
{
    public class PorpoiseSighting
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }
        public DateTime Time { get; set; }
    }
}
