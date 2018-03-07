using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic
{
    public interface IPorpoiseManagementService
    {
        Task<List<Porpoise>> GetPorpoisesAsync();
        Task AddPorpoiseAsync(Porpoise porpoise);
        Task UpdatePorpoiseAsync(Porpoise porpoise);
    }
}
