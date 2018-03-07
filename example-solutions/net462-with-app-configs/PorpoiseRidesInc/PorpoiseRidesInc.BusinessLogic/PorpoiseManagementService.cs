using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.BusinessLogic.InfrastructureContracts;
using PorpoiseRidesInc.BusinessLogic.Validation;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic
{
    public class PorpoiseManagementService : IPorpoiseManagementService
    {
        private IPorpoiseRepository _porpoiseRepository;
        private IPorpoiseValidator _porpoiseValidator;

        public PorpoiseManagementService(IPorpoiseRepository porpoiseRepository,
            IPorpoiseValidator porpoiseValidator)
        {
            _porpoiseRepository = porpoiseRepository;
            _porpoiseValidator = porpoiseValidator;
        }

        public async Task AddPorpoiseAsync(Porpoise porpoise)
        {
            if(await _porpoiseValidator.IsValidPorpoiseAsync(porpoise))
                await _porpoiseRepository.AddAsync(porpoise);

            throw new Exception("Control flow via exception hurray!");
        }

        public async Task<List<Porpoise>> GetPorpoisesAsync()
        {
            return await _porpoiseRepository.GetAsync();
        }

        public async Task UpdatePorpoiseAsync(Porpoise porpoise)
        {
            await _porpoiseRepository.UpdateAsync(porpoise);
        }
    }
}
