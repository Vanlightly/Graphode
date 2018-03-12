using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WhaleSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.WhaleRecognition
{
    public delegate SpeciesModel LoadModel();

    public class SpeciesModel
    {
        public string Species { get; set; }
        public string SomeAiModel { get; set; }
    }

    public interface IWhaleRecognizer
    {
        RecognitionResult Recognize(WhaleSighting sighting);
    }
}
