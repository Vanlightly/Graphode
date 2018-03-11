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
    public class PorpoiseSightingsController : ApiController
    {
        private IPorpoiseSightingManagementService _porpoiseSightingManagementService;

        public PorpoiseSightingsController(IPorpoiseSightingManagementService sightingManagementService)
        {
            _porpoiseSightingManagementService = sightingManagementService;
        }

        // GET: api/PorpoiseSightings
        public async Task<IEnumerable<PorpoiseSightingDTO>> GetAsync()
        {
            var sightings = await _porpoiseSightingManagementService.GetPorpoiseSightingsAsync();

            return sightings.Select(x => new PorpoiseSightingDTO()
            {
                Name = x.Name,
                Species = x.Species,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Time = x.Time
            }).ToList();
        }

        // POST: api/PorpoiseSightings
        public async Task PostAsync([FromBody]PorpoiseSightingDTO porpoise)
        {
            PorpoiseSighting porpoiseEntity = Convert(porpoise);
            await _porpoiseSightingManagementService.AddSightingAsync(porpoiseEntity);
        }

        // PUT: api/PorpoiseSightings/5
        public async Task PutAsync(int id, [FromBody]PorpoiseSightingDTO sighting)
        {
            PorpoiseSighting sightingEntity = new PorpoiseSighting()
            {
                Name = sighting.Name,
                Species = sighting.Species,
                Latitude = sighting.Latitude,
                Longitude = sighting.Longitude,
                Time = sighting.Time
            };

            await _porpoiseSightingManagementService.UpdatePorpoiseSightingAsync(sightingEntity);
        }

        private PorpoiseSighting Convert(PorpoiseSightingDTO sighting)
        {
            return new PorpoiseSighting()
            {
                Name = sighting.Name,
                Species = sighting.Species,
                Latitude = sighting.Latitude,
                Longitude = sighting.Longitude,
                Time = sighting.Time
            };
        }
    }
}
