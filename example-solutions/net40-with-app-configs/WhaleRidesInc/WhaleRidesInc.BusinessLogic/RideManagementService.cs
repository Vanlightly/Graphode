using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;
using WhaleRidesInc.BusinessLogic.InfrastructureContracts;

namespace WhaleRidesInc.BusinessLogic
{
    public class RideManagementService : IRideManagementService
    {
        private IRidesRepository _ridesRepository;

        public RideManagementService(IRidesRepository rideRepository)
        {
            _ridesRepository = rideRepository;
        }

        public void AddRide(Ride ride)
        {
            _ridesRepository.Add(ride);
        }

        public List<Ride> GetRides()
        {
            return _ridesRepository.Get();
        }

        public void UpdateRide(Ride ride)
        {
            _ridesRepository.Update(ride);
        }
    }
}
