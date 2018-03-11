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
    public class SightingTripsRepository : ISightingTripRepository
    {
        public async Task AddAsync(SightingTrip sightingTrip)
        {
            using (var context = new PorpoiseSightingsContext())
            {
                context.SightingTrips.Add(sightingTrip);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<SightingTrip>> GetAsync()
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.SightingTrips.ToListAsync();
            }
        }

        public async Task UpdateAsync(SightingTrip sightingTrip)
        {
            using (var context = new PorpoiseSightingsContext())
            {
                var sightingTripDb = await context.SightingTrips.FirstOrDefaultAsync(x => x.Id == sightingTrip.Id);
                if(sightingTripDb != null)
                {
                    sightingTripDb.Captain = sightingTrip.Captain;
                    sightingTripDb.DepartureTime = sightingTrip.DepartureTime;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
