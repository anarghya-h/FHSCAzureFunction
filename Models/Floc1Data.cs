using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class Floc1Data: PbsObject
    {
        [JsonProperty(PropertyName = "Terminal_Description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "Country_Code")]
        public string Country { get; set; }
        public SDAUnit SDAUnit { get; set; }
    }
}
