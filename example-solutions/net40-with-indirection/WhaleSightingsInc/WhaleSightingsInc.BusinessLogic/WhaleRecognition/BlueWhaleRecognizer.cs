using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhaleSightingsInc.BusinessLogic.Entities;

namespace WhaleSightingsInc.BusinessLogic.WhaleRecognition
{
    public class BlueWhaleRecognizer : IWhaleRecognizer
    {
        private LoadModel _modelLoader;

        public BlueWhaleRecognizer(LoadModel modelLoader)
        {
            _modelLoader = modelLoader;
        }

        public RecognitionResult Recognize(WhaleSighting sighting)
        {
            var model = _modelLoader();

            // some amazing whale recognition code based on our whale model and specific to blue whales
            //...

            return new RecognitionResult();
        }
    }
}
