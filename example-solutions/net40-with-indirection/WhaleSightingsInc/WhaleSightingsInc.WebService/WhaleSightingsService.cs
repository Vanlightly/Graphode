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
    public class WhaleSightingsService : IWhaleSightingsService
    {
        private IWhaleSightingManagementService _whaleSightingManagementService;

        public WhaleSightingsService(IWhaleSightingManagementService whaleSightingManagementService)
        {
            _whaleSightingManagementService = whaleSightingManagementService;
        }

        public void AddWhale(WhaleSightingDTO whaleSighting)
        {
            WhaleSighting sightingEntity = Convert(whaleSighting);
            _whaleSightingManagementService.AddSighting(sightingEntity);
        }

        public List<WhaleSightingDTO> GetWhales()
        {
            var sightings = _whaleSightingManagementService.GetWhaleSightings();

            return sightings.Select(x => new WhaleSightingDTO()
            {
                Name = x.Name,
                Species = x.Species,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Time = x.Time
            }).ToList();
        }

        public void UpdateWhale(WhaleSightingDTO whaleSighting)
        {
            WhaleSighting whaleSightingEntity = new WhaleSighting()
            {
                Name = whaleSighting.Name,
                Species = whaleSighting.Species,
                Latitude = whaleSighting.Latitude,
                Longitude = whaleSighting.Longitude,
                Time = whaleSighting.Time
            };

            _whaleSightingManagementService.UpdateWhaleSighting(whaleSightingEntity);
        }

        private WhaleSighting Convert(WhaleSightingDTO whaleSighting)
        {
            return new WhaleSighting()
            {
                Name = whaleSighting.Name,
                Species = whaleSighting.Species,
                Latitude = whaleSighting.Latitude,
                Longitude = whaleSighting.Longitude,
                Time = whaleSighting.Time,
            };
        }
    }
}
