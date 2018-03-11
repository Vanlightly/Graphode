using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic
{
    public interface ISightingTripManagementService
    {
        Task<List<SightingTrip>> GetSightingTripsAsync();
        Task AddSightingTripAsync(SightingTrip porpoise);
        Task UpdateSightingTripAsync(SightingTrip porpoise);
    }
}
