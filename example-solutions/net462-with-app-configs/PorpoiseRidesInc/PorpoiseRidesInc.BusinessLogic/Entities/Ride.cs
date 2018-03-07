using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PorpoiseRidesInc.BusinessLogic.Entities
{
    public class Ride
    {
        public int Id { get; set; }
        public int PorpoiseId { get; set; }
        public string Rider { get; set; }
        public DateTime RideTime { get; set; }
        public Porpoise Porpoise { get; set; }
    }
}
