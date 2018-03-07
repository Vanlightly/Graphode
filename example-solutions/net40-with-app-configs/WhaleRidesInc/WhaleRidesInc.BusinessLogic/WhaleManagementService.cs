using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleRidesInc.BusinessLogic.Entities;
using WhaleRidesInc.BusinessLogic.InfrastructureContracts;
using WhaleRidesInc.BusinessLogic.Validation;

namespace WhaleRidesInc.BusinessLogic
{
    public class WhaleManagementService : IWhaleManagementService
    {
        private IWhaleRepository _whaleRepository;
        private IWhaleValidator _whaleValidator;

        public WhaleManagementService(IWhaleRepository whaleRepository,
            IWhaleValidator whaleValidator)
        {
            _whaleRepository = whaleRepository;
            _whaleValidator = whaleValidator;
        }

        public void AddWhale(Whale whale)
        {
            if(_whaleValidator.IsValidWhale(whale))
                _whaleRepository.Add(whale);

            throw new Exception("Control flow via exception hurray!");
        }

        public List<Whale> GetWhales()
        {
            return _whaleRepository.Get();
        }

        public void UpdateWhale(Whale whale)
        {
            _whaleRepository.Update(whale);
        }
    }
}
