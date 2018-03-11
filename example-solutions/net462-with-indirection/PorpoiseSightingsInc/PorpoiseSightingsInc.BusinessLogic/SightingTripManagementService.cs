using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic
{
    public class SightingTripManagementService : ISightingTripManagementService
    {
        private ISightingTripRepository _sightingTripRepository;

        public SightingTripManagementService(ISightingTripRepository sightingTripRepository)
        {
            _sightingTripRepository = sightingTripRepository;
        }

        public async Task AddSightingTripAsync(SightingTrip sightingTrip)
        {
            await _sightingTripRepository.AddAsync(sightingTrip);
        }

        public async Task<List<SightingTrip>> GetSightingTripsAsync()
        {
            return await _sightingTripRepository.GetAsync();
        }

        public async Task UpdateSightingTripAsync(SightingTrip sightingTrip)
        {
            await _sightingTripRepository.UpdateAsync(sightingTrip);
        }
    }
}
