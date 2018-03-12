using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic
{
    public interface ISightingTripManagementService
    {
        List<SightingTrip> GetSightingTrips();
        void AddSightingTrip(SightingTrip whale);
        void UpdateSightingTrip(SightingTrip whale);
    }
}
