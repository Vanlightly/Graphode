using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.BusinessLogic
{
    public interface IRideManagementService
    {
        List<Ride> GetRides();
        void AddRide(Ride whale);
        void UpdateRide(Ride whale);
    }
}
