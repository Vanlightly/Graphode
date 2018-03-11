namespace PorpoiseSightingsInc.Infrastructure
{
    using BusinessLogic.Entities;
    using BusinessLogic.PorpoiseRecognition;
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class PorpoiseSightingsContext : DbContext
    {
        public PorpoiseSightingsContext()
            : base("name=PorpoiseSightings")
        {
        }

        public virtual DbSet<PorpoiseSighting> PorpoiseSightings { get; set; }
        public virtual DbSet<SightingTrip> SightingTrips { get; set; }
        public virtual DbSet<SpeciesModel> SpeciesModels { get; set; }
    }


}