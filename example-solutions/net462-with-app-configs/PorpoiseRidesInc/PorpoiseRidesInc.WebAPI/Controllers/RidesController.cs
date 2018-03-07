using PorpoiseRidesInc.BusinessLogic;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.WebAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PorpoiseRidesInc.WebAPI.Controllers
{
    // imagine I had set up Autofac
    public class RidesController : ApiController
    {
        private IRideManagementService _rideManagementService;

        public RidesController(IRideManagementService rideManagementService)
        {
            _rideManagementService = rideManagementService;
        }

        // GET: api/Rides
        public async Task<IEnumerable<RideDTO>> GetAsync()
        {
            var rides = await _rideManagementService.GetRidesAsync();

            return rides.Select(x => new RideDTO()
            {
                Rider = x.Rider,
                Time = x.RideTime,
                Porpoise = new PorpoiseDTO()
                {
                    Name = x.Porpoise.Name,
                    Species = x.Porpoise.Species
                }
            }).ToList();
        }

        // POST: api/Rides
        public async Task PostAsync([FromBody]RideDTO ride)
        {
            Ride rideEntity = new Ride()
            {
                Rider = ride.Rider,
                RideTime = ride.Time,
                Porpoise = new Porpoise()
                {
                    Name = ride.Porpoise.Name,
                    Species = ride.Porpoise.Species
                }
            };

            await _rideManagementService.AddRideAsync(rideEntity);
        }

        // PUT: api/Rides/5
        public async Task PutAsync(int id, [FromBody]RideDTO ride)
        {
            Ride rideEntity = new Ride()
            {
                Id = id,
                Rider = ride.Rider,
                RideTime = ride.Time,
                Porpoise = new Porpoise()
                {
                    Name = ride.Porpoise.Name,
                    Species = ride.Porpoise.Species
                }
            };

            await _rideManagementService.UpdateRideAsync(rideEntity);
        }
    }
}
