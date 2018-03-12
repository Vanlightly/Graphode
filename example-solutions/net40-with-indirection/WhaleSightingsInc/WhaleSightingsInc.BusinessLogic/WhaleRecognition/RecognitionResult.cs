using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhaleSightingsInc.BusinessLogic.WhaleRecognition
{
    public class RecognitionResult
    {
        public bool IsMatch { get; set; }
        public string Species { get; set; }
    }
}
