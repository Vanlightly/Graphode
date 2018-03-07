using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.BusinessLogic
{
    public interface IWhaleManagementService
    {
        List<Whale> GetWhales();
        void AddWhale(Whale whale);
        void UpdateWhale(Whale whale);
    }
}
