using PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface IRecognitionModelsRepository
    {
        Task<SpeciesModel> LoadHarbourPorpoiseModelAsync();
        Task<SpeciesModel> LoadSpectacledPorpoiseModelAsync();
        Task<SpeciesModel> LoadFinlessPorpoiseModelAsync();
    }
}
