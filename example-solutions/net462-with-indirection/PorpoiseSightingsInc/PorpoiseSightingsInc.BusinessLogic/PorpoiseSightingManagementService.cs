using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts;
using System.Threading.Tasks;
using PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition;

namespace PorpoiseSightingsInc.BusinessLogic
{
    public class PorpoiseSightingManagementService : IPorpoiseSightingManagementService
    {
        private IPorpoiseSightingRepository _porpoiseSightingRepository;
        private IRecognitionModelsRepository _recognitionModelsRepository;

        public PorpoiseSightingManagementService(IPorpoiseSightingRepository porpoiseRepository,
            IRecognitionModelsRepository recognitionModelsRepository)
        {
            _porpoiseSightingRepository = porpoiseRepository;
            _recognitionModelsRepository = recognitionModelsRepository;
        }

        public async Task AddSightingAsync(PorpoiseSighting sighting)
        {
            foreach(var recognizer in PorpoiseRecognitionFactory.BuildRecognizers(_recognitionModelsRepository))
            {
                var result = await recognizer.RecognizeAsync(sighting);
                if (result.IsMatch)
                    sighting.Species = result.Species;
            }
            
            await _porpoiseSightingRepository.AddAsync(sighting);
        }

        public async Task<List<PorpoiseSighting>> GetPorpoiseSightingsAsync()
        {
            return await _porpoiseSightingRepository.GetAsync();
        }

        public async Task UpdatePorpoiseSightingAsync(PorpoiseSighting porpoiseSighting)
        {
            await _porpoiseSightingRepository.UpdateAsync(porpoiseSighting);
        }
    }
}
