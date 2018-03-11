using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface ISightingTripRepository
    {
        Task<List<SightingTrip>> GetAsync();
        Task AddAsync(SightingTrip sighting);
        Task UpdateAsync(SightingTrip sighting);
    }
}
