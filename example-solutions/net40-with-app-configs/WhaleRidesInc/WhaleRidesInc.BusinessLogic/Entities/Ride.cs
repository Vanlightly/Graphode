using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhaleRidesInc.BusinessLogic.Entities
{
    public class Ride
    {
        public int Id { get; set; }
        public int WhaleId { get; set; }
        public string Rider { get; set; }
        public DateTime RideTime { get; set; }
        public Whale Whale { get; set; }
    }
}
