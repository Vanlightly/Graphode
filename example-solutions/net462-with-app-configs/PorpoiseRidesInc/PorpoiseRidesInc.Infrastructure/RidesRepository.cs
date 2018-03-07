using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;
using System.Data.Entity;

namespace PorpoiseRidesInc.Infrastructure
{
    public class RidesRepository : IRidesRepository
    {
        public async Task AddAsync(Ride ride)
        {
            using (var context = new PorpoiseRidesContext())
            {
                context.Rides.Add(ride);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<Ride>> GetAsync()
        {
            using (var context = new PorpoiseRidesContext())
            {
                return await context.Rides.ToListAsync();
            }
        }

        public async Task UpdateAsync(Ride ride)
        {
            using (var context = new PorpoiseRidesContext())
            {
                var rideDb = context.Rides.FirstOrDefault(x => x.Id == ride.Id);
                rideDb.PorpoiseId = ride.PorpoiseId;
                rideDb.Rider = ride.Rider;
                rideDb.RideTime = ride.RideTime;
                await context.SaveChangesAsync();
            }
        }
    }
}
