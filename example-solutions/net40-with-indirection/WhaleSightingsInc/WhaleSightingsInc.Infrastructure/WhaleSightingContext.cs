namespace WhaleSightingsInc.Infrastructure
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using WhaleSightingsInc.BusinessLogic.Entities;
    using WhaleSightingsInc.BusinessLogic.WhaleRecognition;

    public class WhaleSightingsContext : DbContext
    {
        public WhaleSightingsContext()
            : base("name=WhaleSightingsInc")
        {
        }
        public virtual DbSet<SightingTrip> SightingTrips { get; set; }
        public virtual DbSet<WhaleSighting> WhaleSightings { get; set; }
        public virtual DbSet<SpeciesModel> SpeciesModels { get; set; }
    }
}