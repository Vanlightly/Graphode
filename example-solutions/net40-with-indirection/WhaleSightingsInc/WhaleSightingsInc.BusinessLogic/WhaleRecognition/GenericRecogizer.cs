using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhaleSightingsInc.BusinessLogic.Entities;

namespace WhaleSightingsInc.BusinessLogic.WhaleRecognition
{
    public class GenericRecogizer : IWhaleRecognizer
    {
        private LoadModel _modelLoader;

        public GenericRecogizer(LoadModel modelLoader)
        {
            _modelLoader = modelLoader;
        }

        public RecognitionResult Recognize(WhaleSighting sighting)
        {
            var model = _modelLoader();

            // some amazing whale recognition code based on our whale model that can be applied to multiple species
            //...

            return new RecognitionResult();
        }
    }
}
