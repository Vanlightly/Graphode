using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic
{
    public interface IRideManagementService
    {
        Task<List<Ride>> GetRidesAsync();
        Task AddRideAsync(Ride porpoise);
        Task UpdateRideAsync(Ride porpoise);
    }
}
