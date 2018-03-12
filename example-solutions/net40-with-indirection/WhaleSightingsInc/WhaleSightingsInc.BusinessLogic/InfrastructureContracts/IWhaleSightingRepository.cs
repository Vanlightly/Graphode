using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface IWhaleSightingRepository
    {
        List<WhaleSighting> Get();
        int GetCount(string species);
        void Add(WhaleSighting whale);
        void Update(WhaleSighting whale);
    }
}
