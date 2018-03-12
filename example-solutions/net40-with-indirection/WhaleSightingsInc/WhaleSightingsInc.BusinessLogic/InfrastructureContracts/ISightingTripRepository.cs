using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface ISightingTripRepository
    {
        List<SightingTrip> Get();
        void Add(SightingTrip sighting);
        void Update(SightingTrip sighting);
    }
}
