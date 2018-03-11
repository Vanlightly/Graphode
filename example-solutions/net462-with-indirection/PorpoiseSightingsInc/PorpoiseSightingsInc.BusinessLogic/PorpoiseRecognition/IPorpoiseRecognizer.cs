using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PorpoiseSightingsInc.BusinessLogic.Entities;
using System.Threading.Tasks;

namespace PorpoiseSightingsInc.BusinessLogic.PorpoiseRecognition
{
    public delegate Task<SpeciesModel> LoadModelAsync();

    public class SpeciesModel
    {
        public string Species { get; set; }
        public string SomeAiModel { get; set; }
    }

    public interface IPorpoiseRecognizer
    {
        Task<RecognitionResult> RecognizeAsync(PorpoiseSighting sighting);
    }
}
