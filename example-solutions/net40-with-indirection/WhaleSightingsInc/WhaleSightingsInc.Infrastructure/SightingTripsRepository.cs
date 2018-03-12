using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;

namespace WhaleSightingsInc.Infrastructure
{
    public class SightingTripRepository : ISightingTripRepository
    {
        public void Add(SightingTrip trip)
        {
            using (var context = new WhaleSightingsContext())
            {
                context.SightingTrips.Add(trip);
                context.SaveChanges();
            }
        }

        public List<SightingTrip> Get()
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.SightingTrips.ToList();
            }
        }

        public void Update(SightingTrip trip)
        {
            using (var context = new WhaleSightingsContext())
            {
                var sightingTripDb = context.SightingTrips.FirstOrDefault(x => x.Id == trip.Id);
                sightingTripDb.Captain = trip.Captain;
                sightingTripDb.DepartureTime = trip.DepartureTime;

                context.SaveChanges();
            }
        }
    }
}
