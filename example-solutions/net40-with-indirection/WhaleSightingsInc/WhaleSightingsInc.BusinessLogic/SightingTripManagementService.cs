using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic
{
    public class SightingTripManagementService : ISightingTripManagementService
    {
        private ISightingTripRepository _sightingTripRepository;

        public SightingTripManagementService(ISightingTripRepository sightingTripRepository)
        {
            _sightingTripRepository = sightingTripRepository;
        }

        public void AddSightingTrip(SightingTrip sightingTrip)
        {
            _sightingTripRepository.Add(sightingTrip);
        }

        public List<SightingTrip> GetSightingTrips()
        {
            return _sightingTripRepository.Get();
        }

        public void UpdateSightingTrip(SightingTrip sightingTrip)
        {
            _sightingTripRepository.Update(sightingTrip);
        }
    }
}
