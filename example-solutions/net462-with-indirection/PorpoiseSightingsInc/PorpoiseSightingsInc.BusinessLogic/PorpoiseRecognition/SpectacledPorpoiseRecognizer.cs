using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PorpoiseSightingsInc.BusinessLogic.Entities;

namespace PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition
{
    public class SpectacledPorpoiseRecognizer : IPorpoiseRecognizer
    {
        private LoadModelAsync _modelLoader;

        public SpectacledPorpoiseRecognizer(LoadModelAsync modelLoader)
        {
            _modelLoader = modelLoader;
        }

        public async Task<RecognitionResult> RecognizeAsync(PorpoiseSighting sighting)
        {
            var model = await _modelLoader();

            // some amazing porpoise recognition code based on our porpoise model and specific to spectacled porpoises
            //...

            return new RecognitionResult();
        }
    }
}
