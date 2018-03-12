using WhaleSightingsInc.BusinessLogic.WhaleRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.InfrastructureContracts
{
    public interface IRecognitionModelsRepository
    {
        SpeciesModel LoadFinWhaleModel();
        SpeciesModel LoadHumpBackWhaleModel();
        SpeciesModel LoadBlueWhaleModel();
    }
}
