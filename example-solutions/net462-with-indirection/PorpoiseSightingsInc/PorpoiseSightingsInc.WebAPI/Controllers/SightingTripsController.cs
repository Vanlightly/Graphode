using PorpoiseSightingsInc.BusinessLogic;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using PorpoiseSightingsInc.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PorpoiseSightingsInc.WebAPI.Controllers
{
    // imagine I had set up Autofac
    public class SightingTripsController : ApiController
    {
        private ISightingTripManagementService _sightingTripManagementService;

        public SightingTripsController(ISightingTripManagementService sightingsManagementService)
        {
            _sightingTripManagementService = sightingsManagementService;
        }

        // GET: api/SightingTrips
        public async Task<IEnumerable<SightingTripDTO>> GetAsync()
        {
            var sightingTrips = await _sightingTripManagementService.GetSightingTripsAsync();

            return sightingTrips.Select(x => new SightingTripDTO()
            {
                Captain = x.Captain,
                DepartureTime = x.DepartureTime,
                Sightings = x.PorpoiseSightings.Select(s => new PorpoiseSightingDTO()
                {
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Name = s.Name,
                    Species = s.Species,
                    Time = s.Time
                }).ToList()
            }).ToList();
        }

        // POST: api/SightingTrips
        public async Task PostAsync([FromBody]SightingTripDTO sightingTrip)
        {
            SightingTrip tripEntity = new SightingTrip()
            {
                Captain = sightingTrip.Captain,
                DepartureTime = sightingTrip.DepartureTime,
                PorpoiseSightings = sightingTrip.Sightings.Select(s => new PorpoiseSighting()
                {
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Name = s.Name,
                    Species = s.Species,
                    Time = s.Time
                }).ToList()
            };

            await _sightingTripManagementService.AddSightingTripAsync(tripEntity);
        }

        // PUT: api/SightingTrips/5
        public async Task PutAsync(int id, [FromBody]SightingTripDTO sightingTrip)
        {
            SightingTrip tripEntity = new SightingTrip()
            {
                Captain = sightingTrip.Captain,
                DepartureTime = sightingTrip.DepartureTime,
                PorpoiseSightings = sightingTrip.Sightings.Select(s => new PorpoiseSighting()
                {
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Name = s.Name,
                    Species = s.Species,
                    Time = s.Time
                }).ToList()
            };

            await _sightingTripManagementService.UpdateSightingTripAsync(tripEntity);
        }
    }
}
