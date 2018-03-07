using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;
using WhaleRidesInc.BusinessLogic.InfrastructureContracts;

namespace WhaleRidesInc.Infrastructure
{
    public class RidesRepository : IRidesRepository
    {
        public void Add(Ride ride)
        {
            using (var context = new WhaleRides())
            {
                context.Rides.Add(ride);
                context.SaveChanges();
            }
        }

        public List<Ride> Get()
        {
            using (var context = new WhaleRides())
            {
                return context.Rides.ToList();
            }
        }

        public void Update(Ride ride)
        {
            using (var context = new WhaleRides())
            {
                var rideDb = context.Rides.FirstOrDefault(x => x.Id == ride.Id);
                rideDb.WhaleId = ride.WhaleId;
                rideDb.Rider = ride.Rider;
                rideDb.RideTime = ride.RideTime;
                context.SaveChanges();
            }
        }
    }
}
