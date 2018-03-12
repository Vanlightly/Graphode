using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;
using WhaleSightingsInc.BusinessLogic.WhaleRecognition;

namespace WhaleSightingsInc.BusinessLogic
{
    public class WhaleSightingManagementService : IWhaleSightingManagementService
    {
        private IWhaleSightingRepository _whaleSightingRepository;
        private IRecognitionModelsRepository _recognitionModelsRepository;

        public WhaleSightingManagementService(IWhaleSightingRepository whaleRepository,
            IRecognitionModelsRepository recognitionModelsRepository)
        {
            _whaleSightingRepository = whaleRepository;
            _recognitionModelsRepository = recognitionModelsRepository;
        }

        public void AddSighting(WhaleSighting sighting)
        {
            foreach(var recognizer in WhaleRecognitionFactory.BuildRecognizers(_recognitionModelsRepository))
            {
                var result = recognizer.Recognize(sighting);
                if (result.IsMatch)
                    sighting.Species = result.Species;
            }
            
            _whaleSightingRepository.Add(sighting);
        }

        public List<WhaleSighting> GetWhaleSightings()
        {
            return _whaleSightingRepository.Get();
        }

        public void UpdateWhaleSighting(WhaleSighting whaleSighting)
        {
            _whaleSightingRepository.Update(whaleSighting);
        }
    }
}
