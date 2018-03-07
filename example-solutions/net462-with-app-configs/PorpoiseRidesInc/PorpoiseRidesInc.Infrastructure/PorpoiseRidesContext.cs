namespace PorpoiseRidesInc.Infrastructure
{
    using BusinessLogic.Entities;
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class PorpoiseRidesContext : DbContext
    {
        public PorpoiseRidesContext()
            : base("name=PorpoiseRides")
        {
        }

        public virtual DbSet<Porpoise> Porpoises { get; set; }
        public virtual DbSet<Ride> Rides { get; set; }
    }


}