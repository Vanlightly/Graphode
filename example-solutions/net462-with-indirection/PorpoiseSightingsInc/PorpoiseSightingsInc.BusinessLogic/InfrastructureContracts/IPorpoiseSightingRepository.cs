using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface IPorpoiseSightingRepository
    {
        Task<List<PorpoiseSighting>> GetAsync();
        Task<int> GetCountAsync(string species);
        Task AddAsync(PorpoiseSighting porpoise);
        Task UpdateAsync(PorpoiseSighting porpoise);
    }
}
