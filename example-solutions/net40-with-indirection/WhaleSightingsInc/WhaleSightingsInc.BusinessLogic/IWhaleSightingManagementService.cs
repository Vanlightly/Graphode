using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic
{
    public interface IWhaleSightingManagementService
    {
        List<WhaleSighting> GetWhaleSightings();
        void AddSighting(WhaleSighting whaleSighting);
        void UpdateWhaleSighting(WhaleSighting whaleSighting);
    }
}
