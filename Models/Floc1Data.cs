﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class Floc1Data: PbsObject
    {
        public string Description { get; set; }
        public string Country { get; set; }
        public SDAUnit SDAUnit { get; set; }
    }
}
