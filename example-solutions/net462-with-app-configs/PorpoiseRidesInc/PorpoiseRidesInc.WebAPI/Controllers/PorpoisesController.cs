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
    public class PorpoisesController : ApiController
    {
        private IPorpoiseManagementService _porpoiseManagementService;

        public PorpoisesController(IPorpoiseManagementService rideManagementService)
        {
            _porpoiseManagementService = rideManagementService;
        }

        // GET: api/Porpoises
        public async Task<IEnumerable<PorpoiseDTO>> GetAsync()
        {
            var rides = await _porpoiseManagementService.GetPorpoisesAsync();

            return rides.Select(x => new PorpoiseDTO()
            {
                Name = x.Name,
                Species = x.Species
            }).ToList();
        }

        // POST: api/Porpoises
        public async Task PostAsync([FromBody]PorpoiseDTO porpoise)
        {
            Porpoise porpoiseEntity = Convert(porpoise);
            await _porpoiseManagementService.AddPorpoiseAsync(porpoiseEntity);
        }

        // PUT: api/Porpoises/5
        public async Task PutAsync(int id, [FromBody]PorpoiseDTO porpoise)
        {
            Porpoise porpoiseEntity = new Porpoise()
            {
                Name = porpoise.Name,
                Species = porpoise.Species
            };

            await _porpoiseManagementService.UpdatePorpoiseAsync(porpoiseEntity);
        }

        private Porpoise Convert(PorpoiseDTO porpoise)
        {
            return new Porpoise()
            {
                Name = porpoise.Name,
                Species = porpoise.Species,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}
