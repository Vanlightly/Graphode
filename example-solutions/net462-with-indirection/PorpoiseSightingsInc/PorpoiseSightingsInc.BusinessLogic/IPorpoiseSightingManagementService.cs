using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic
{
    public interface IPorpoiseSightingManagementService
    {
        Task<List<PorpoiseSighting>> GetPorpoiseSightingsAsync();
        Task AddSightingAsync(PorpoiseSighting porpoise);
        Task UpdatePorpoiseSightingAsync(PorpoiseSighting porpoise);
    }
}
