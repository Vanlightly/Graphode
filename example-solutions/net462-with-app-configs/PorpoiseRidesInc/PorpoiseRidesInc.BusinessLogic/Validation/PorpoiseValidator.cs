using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseRidesInc.BusinessLogic.Entities;
using PorpoiseRidesInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;

namespace PorpoiseRidesInc.BusinessLogic.Validation
{
    public class PorpoiseValidator : IPorpoiseValidator
    {
        private readonly IPorpoiseRepository _porpoiseRepository;

        public PorpoiseValidator(IPorpoiseRepository porpoiseRepository)
        {
            _porpoiseRepository = porpoiseRepository;
        }

        public async Task<bool> IsValidPorpoiseAsync(Porpoise porpoise)
        {
            var speciesCount = await GetNumberOfPorpoisesInSpeciesAsync(porpoise);
            if (speciesCount > 10)
                return false;

            return true;
        }

        private async Task<int> GetNumberOfPorpoisesInSpeciesAsync(Porpoise porpoise)
        {
            return await _porpoiseRepository.GetCountAsync(porpoise.Species);
        }
    }
}
