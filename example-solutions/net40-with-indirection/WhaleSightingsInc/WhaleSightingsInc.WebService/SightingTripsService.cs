using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using WhaleSightingsInc.BusinessLogic;
using WhaleSightingsInc.BusinessLogic.Entities;

namespace WhaleSightingsInc.WebService
{
    // Let's imagine I hooked up Autofac
    public class SightingTripsService : ISightingTripsService
    {
        private ISightingTripManagementService _sightingTripManagementService;

        public SightingTripsService(ISightingTripManagementService sightingTripManagementService)
        {
            _sightingTripManagementService = sightingTripManagementService;
        }

        public void AddSightingTrip(SightingTripDTO sightingTrip)
        {
            SightingTrip tripEntity = new SightingTrip()
            {
                Captain = sightingTrip.Captain,
                DepartureTime = sightingTrip.DepartureTime,
                WhaleSightings = sightingTrip.Sightings.Select(x => new 
                    WhaleSighting()
                    {
                        Latitude = x.Latitude,
                        Longitude = x.Longitude,
                        Name = x.Name,
                        Species = x.Species,
                        Time = x.Time
                    }).ToList()
            };

            _sightingTripManagementService.AddSightingTrip(tripEntity);
        }

        public List<SightingTripDTO> GetSightingTrips()
        {
            var trips = _sightingTripManagementService.GetSightingTrips();

            return trips.Select(x => new SightingTripDTO()
            {
                Captain = x.Captain,
                DepartureTime = x.DepartureTime,
                Sightings = x.WhaleSightings.Select(s => 
                    new WhaleSightingDTO()
                    {
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        Name = s.Name,
                        Species = s.Species,
                        Time = s.Time
                    }).ToList()
            }).ToList();
        }

        public void UpdateSightingTrip(SightingTripDTO sightingTrip)
        {
            SightingTrip tripEntity = new SightingTrip()
            {
                Captain = sightingTrip.Captain,
                DepartureTime = sightingTrip.DepartureTime,
                WhaleSightings = sightingTrip.Sightings.Select(x => new
                    WhaleSighting()
                {
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Name = x.Name,
                    Species = x.Species,
                    Time = x.Time
                }).ToList()
            };

            _sightingTripManagementService.UpdateSightingTrip(tripEntity);
        }
    }
}
