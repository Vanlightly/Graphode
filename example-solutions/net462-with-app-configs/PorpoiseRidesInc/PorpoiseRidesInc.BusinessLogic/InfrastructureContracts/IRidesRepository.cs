using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic.InfrastructureContracts
{
    public interface IRidesRepository
    {
        Task<List<Ride>> GetAsync();
        Task AddAsync(Ride ride);
        Task UpdateAsync(Ride ride);
    }
}
