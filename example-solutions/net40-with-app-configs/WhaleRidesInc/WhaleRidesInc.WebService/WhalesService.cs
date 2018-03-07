using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using WhaleRidesInc.BusinessLogic;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.WebService
{
    // Let's imagine I hooked up Autofac
    public class WhalesService : IWhalesService
    {
        private IWhaleManagementService _whaleManagementService;

        public WhalesService(IWhaleManagementService whaleManagementService)
        {
            _whaleManagementService = whaleManagementService;
        }

        public void AddWhale(WhaleDTO whale)
        {
            Whale whaleEntity = new Whale()
            {
                Name = whale.Name,
                Species = whale.Species,
                CreatedDate = DateTime.UtcNow
            };

            _whaleManagementService.AddWhale(whaleEntity);
        }

        public List<WhaleDTO> GetWhales()
        {
            var whales = _whaleManagementService.GetWhales();

            return whales.Select(x => new WhaleDTO()
            {
                Name = x.Name,
                Species = x.Species
            }).ToList();
        }

        public void UpdateWhale(WhaleDTO whale)
        {
            Whale whaleEntity = new Whale()
            {
                Name = whale.Name,
                Species = whale.Species,
                CreatedDate = DateTime.UtcNow
            };

            _whaleManagementService.UpdateWhale(whaleEntity);
        }
    }
}
