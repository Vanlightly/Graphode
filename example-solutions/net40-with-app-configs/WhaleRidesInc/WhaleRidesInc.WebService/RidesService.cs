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
    public class RidesService : IRidesService
    {
        private IRideManagementService _ridesManagementService;

        public RidesService(IRideManagementService ridesManagementService)
        {
            _ridesManagementService = ridesManagementService;
        }

        public void AddRide(RideDTO ride)
        {
            Ride rideEntity = new Ride()
            {
                Rider = ride.Rider,
                RideTime = ride.Time,
                Whale = new Whale()
                {
                    Name = ride.Whale.Name,
                    Species = ride.Whale.Species
                }
            };

            _ridesManagementService.AddRide(rideEntity);
        }

        public List<RideDTO> GetRides()
        {
            var rides = _ridesManagementService.GetRides();

            return rides.Select(x => new RideDTO()
            {
                Rider = x.Rider,
                Time = x.RideTime,
                Whale = new WhaleDTO()
                {
                    Name = x.Whale.Name,
                    Species = x.Whale.Species
                }
            }).ToList();
        }

        public void UpdateRide(RideDTO ride)
        {
            Ride rideEntity = new Ride()
            {
                Rider = ride.Rider,
                RideTime = ride.Time,
                Whale = new Whale()
                {
                    Name = ride.Whale.Name,
                    Species = ride.Whale.Species
                }
            };

            _ridesManagementService.UpdateRide(rideEntity);
        }
    }
}
