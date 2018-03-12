using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhaleSightingsInc.Infrastructure;
using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;
using WhaleSightingsInc.BusinessLogic.WhaleRecognition;

namespace WhaleSightingsInc.Infrastructure
{
    public class RecognitionModelsRepository : IRecognitionModelsRepository
    {
        public SpeciesModel LoadFinWhaleModel()
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.SpeciesModels.FirstOrDefault(x => x.Species.Equals("FinWhale"));
            }
        }

        public SpeciesModel LoadHumpBackWhaleModel()
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.SpeciesModels.FirstOrDefault(x => x.Species.Equals("HumpBackWhale"));
            }
        }

        public SpeciesModel LoadBlueWhaleModel()
        {
            using (var context = new WhaleSightingsContext())
            {
                return context.SpeciesModels.FirstOrDefault(x => x.Species.Equals("BlueWhale"));
            }
        }
    }
}
