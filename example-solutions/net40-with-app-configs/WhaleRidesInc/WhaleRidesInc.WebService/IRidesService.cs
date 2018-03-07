using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WhaleRidesInc.WebService
{
    [ServiceContract]
    public interface IRidesService
    {
        [OperationContract]
        List<RideDTO> GetRides();

        [OperationContract]
        void AddRide(RideDTO ride);

        [OperationContract]
        void UpdateRide(RideDTO ride);
    }
}
