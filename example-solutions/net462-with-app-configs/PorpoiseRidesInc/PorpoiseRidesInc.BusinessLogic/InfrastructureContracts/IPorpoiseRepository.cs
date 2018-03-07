using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic.InfrastructureContracts
{
    public interface IPorpoiseRepository
    {
        Task<List<Porpoise>> GetAsync();
        Task<int> GetCountAsync(string species);
        Task AddAsync(Porpoise porpoise);
        Task UpdateAsync(Porpoise porpoise);
    }
}
