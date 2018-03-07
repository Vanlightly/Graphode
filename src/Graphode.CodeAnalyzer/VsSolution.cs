using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Graphode.CodeAnalyzer.Entities;

namespace Graphode.CodeAnalyzer.Code
{
    public class VsSolution
    {
        public VsSolution()
        {
            Applications = new List<ApplicationDetails>();
        }

        public string Name { get; set; }
        public string FolderName { get; set; }
        public List<ApplicationDetails> Applications { get; set; }
    }
}
