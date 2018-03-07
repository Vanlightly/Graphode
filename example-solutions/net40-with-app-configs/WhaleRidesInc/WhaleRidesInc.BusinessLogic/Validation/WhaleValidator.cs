using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;
using WhaleRidesInc.BusinessLogic.InfrastructureContracts;

namespace WhaleRidesInc.BusinessLogic.Validation
{
    public class WhaleValidator : IWhaleValidator
    {
        private readonly IWhaleRepository _whaleRepository;

        public bool IsValidWhale(Whale whale)
        {
            var speciesCount = _whaleRepository.GetCount(whale.Species);
            if (speciesCount > 10)
                return false;

            return true;
        }
    }
}
