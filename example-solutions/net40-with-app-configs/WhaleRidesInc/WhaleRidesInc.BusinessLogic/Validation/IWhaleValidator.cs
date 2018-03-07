using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;

namespace WhaleRidesInc.BusinessLogic.Validation
{
    public interface IWhaleValidator
    {
        bool IsValidWhale(Whale whale);
    }
}
