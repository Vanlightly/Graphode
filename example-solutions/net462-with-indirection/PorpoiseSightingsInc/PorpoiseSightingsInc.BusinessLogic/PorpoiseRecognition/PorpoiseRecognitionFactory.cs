using PorpoiseSightingsInc.BusinessLogic.InfrastructureContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition
{
    public class PorpoiseRecognitionFactory
    {
        public static List<IPorpoiseRecognizer> BuildRecognizers(IRecognitionModelsRepository recognitionModelsRepository)
        {
            var harbourRecognizer = new GenericRecogizer(recognitionModelsRepository.LoadHarbourPorpoiseModelAsync);
            var finlessRecognizer = new GenericRecogizer(recognitionModelsRepository.LoadFinlessPorpoiseModelAsync);
            var spectacledRecognizer = new GenericRecogizer(recognitionModelsRepository.LoadSpectacledPorpoiseModelAsync);

            var recognizers = new List<IPorpoiseRecognizer>();
            recognizers.Add(harbourRecognizer);
            recognizers.Add(finlessRecognizer);
            recognizers.Add(spectacledRecognizer);

            return recognizers;
        }
    }
}
