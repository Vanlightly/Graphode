namespace WhaleRidesInc.Infrastructure
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using BusinessLogic.Entities;

    public class WhaleRides : DbContext
    {
        public WhaleRides()
            : base("name=WhaleRides")
        {
        }
        public virtual DbSet<Whale> Whales { get; set; }
        public virtual DbSet<Ride> Rides { get; set; }
    }
}