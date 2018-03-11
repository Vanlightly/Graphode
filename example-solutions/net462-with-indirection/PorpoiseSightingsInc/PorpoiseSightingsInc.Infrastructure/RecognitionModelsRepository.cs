using PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition;
using System.Data.Entity;

namespace PorpoiseSightingsInc.Infrastructure
{
    /// <summary>
    /// This is of course ridiculous
    /// </summary>
    public class RecognitionModelsRepository : IRecognitionModelsRepository
    {
        public async Task<SpeciesModel> LoadFinlessPorpoiseModelAsync()
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.SpeciesModels.FirstOrDefaultAsync(x => x.Species.Equals("FinlessPorpoise"));
            }
        }

        public async Task<SpeciesModel> LoadHarbourPorpoiseModelAsync()
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.SpeciesModels.FirstOrDefaultAsync(x => x.Species.Equals("HarbourPorpoise"));
            }
        }

        public async Task<SpeciesModel> LoadSpectacledPorpoiseModelAsync()
        {
            using (var context = new PorpoiseSightingsContext())
            {
                return await context.SpeciesModels.FirstOrDefaultAsync(x => x.Species.Equals("SpectacledPorpoise"));
            }
        }
    }
}
