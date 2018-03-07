using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WhaleRidesInc.BusinessLogic.Entities
{
    public class Whale
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Species { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
