using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.BusinessLogic.InfrastructureContracts
{
    public interface IRidesRepository
    {
        List<Ride> Get();
        void Add(Ride ride);
        void Update(Ride ride);
    }
}
