using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WhaleSightingsInc.WebService
{
    [ServiceContract]
    public interface ISightingTripsService
    {
        [OperationContract]
        List<SightingTripDTO> GetSightingTrips();

        [OperationContract]
        void AddSightingTrip(SightingTripDTO sightingTrip);

        [OperationContract]
        void UpdateSightingTrip(SightingTripDTO sightingTrip);
    }
}
