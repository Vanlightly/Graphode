using WhaleSightingsInc.BusinessLogic.InfrastructureContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.WhaleRecognition
{
    public class WhaleRecognitionFactory
    {
        public static List<IWhaleRecognizer> BuildRecognizers(IRecognitionModelsRepository recognitionModelsRepository)
        {
            var humpBackRecognizer = new GenericRecogizer(recognitionModelsRepository.LoadHumpBackWhaleModel);
            var finRecognizer = new GenericRecogizer(recognitionModelsRepository.LoadFinWhaleModel);
            var blueRecognizer = new BlueWhaleRecognizer(recognitionModelsRepository.LoadBlueWhaleModel);

            var recognizers = new List<IWhaleRecognizer>();
            recognizers.Add(humpBackRecognizer);
            recognizers.Add(finRecognizer);
            recognizers.Add(blueRecognizer);

            return recognizers;
        }
    }
}
