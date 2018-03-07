using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic.Validation
{
    public interface IPorpoiseValidator
    {
        Task<bool> IsValidPorpoiseAsync(Porpoise porpoise);
    }
}
