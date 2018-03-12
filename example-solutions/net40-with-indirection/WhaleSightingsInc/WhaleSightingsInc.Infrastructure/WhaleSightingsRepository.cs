using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;

namespace WhaleSightingsInc.Infrastructure
{
    public class WhaleSightingsRepository : IWhaleSightingRepository
    {
        public void Add(WhaleSighting whaleSighting)
        {
            using (var context = new WhaleSightingsContext())
            {
                context.WhaleSightings.Add(whaleSighting);
                context.SaveChanges();
            }
        }

        public List<WhaleSighting> Get()
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.WhaleSightings.ToList();
            }
        }

        public int GetCount(string species)
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.WhaleSightings.Count(x => x.Species.Equals(species));
            }
        }

        public void Update(WhaleSighting whaleSighting)
        {
            using (var context = new WhaleSightingsContext())
            {
                var whaleSightingDb = context.WhaleSightings.FirstOrDefault(x => x.Id == whaleSighting.Id);
                whaleSightingDb.Name = whaleSighting.Name;
                whaleSightingDb.Species = whaleSighting.Species;
                whaleSightingDb.Latitude = whaleSighting.Latitude;
                whaleSightingDb.Longitude = whaleSighting.Longitude;
                whaleSightingDb.Time = whaleSighting.Time;
                context.SaveChanges();
            }
        }
    }
}
