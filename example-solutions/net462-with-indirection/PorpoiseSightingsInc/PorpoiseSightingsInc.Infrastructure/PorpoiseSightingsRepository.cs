using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;
using System.Data.Entity;

namespace PorpoiseSightingsInc.Infrastructure
{
    public class PorpoiseSightingsRepository : IPorpoiseSightingRepository
    {
        public async Task AddAsync(PorpoiseSighting porpoise)
        {
            using (var context = new PorpoiseSightingsContext())
            {
                context.PorpoiseSightings.Add(porpoise);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<PorpoiseSighting>> GetAsync()
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.PorpoiseSightings.ToListAsync();
            }
        }

        public async Task<int> GetCountAsync(string species)
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.PorpoiseSightings.CountAsync(x => x.Species.Equals(species));
            }
        }

        public async Task UpdateAsync(PorpoiseSighting porpoiseSighting)
        {
            using (var context = new PorpoiseSightingsContext())
            {
                var porpoiseSightingDb = context.PorpoiseSightings.FirstOrDefault(x => x.Id == porpoiseSighting.Id);
                porpoiseSightingDb.Name = porpoiseSighting.Name;
                porpoiseSightingDb.Species = porpoiseSighting.Species;
                porpoiseSightingDb.Latitude = porpoiseSighting.Latitude;
                porpoiseSightingDb.Longitude = porpoiseSighting.Longitude;
                porpoiseSightingDb.Time = porpoiseSighting.Time;
                await context.SaveChangesAsync();
            }
        }
    }
}
