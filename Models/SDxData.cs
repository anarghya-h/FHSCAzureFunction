using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class SDxData
    {
        public List<Floc1Data> Floc1Data { get; set; }
        public List<PbsObject> Floc2Data { get; set; }
        public List<PbsObject> Floc3Data { get; set; }
        public List<PbsObject> Floc4Data { get; set; }
    }
}
