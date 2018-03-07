using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.BusinessLogic.InfrastructureContracts
{
    public interface IWhaleRepository
    {
        List<Whale> Get();
        int GetCount(string species);
        void Add(Whale whale);
        void Update(Whale whale);
    }
}
