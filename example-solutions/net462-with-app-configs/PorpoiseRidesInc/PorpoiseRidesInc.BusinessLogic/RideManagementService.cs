using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic
{
    public class RideManagementService : IRideManagementService
    {
        private IRidesRepository _ridesRepository;

        public RideManagementService(IRidesRepository rideRepository)
        {
            _ridesRepository = rideRepository;
        }

        public async Task AddRideAsync(Ride ride)
        {
            await _ridesRepository.AddAsync(ride);
        }

        public async Task<List<Ride>> GetRidesAsync()
        {
            return await _ridesRepository.GetAsync();
        }

        public async Task UpdateRideAsync(Ride ride)
        {
            await _ridesRepository.UpdateAsync(ride);
        }
    }
}
