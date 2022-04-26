using FHSCAzureFunction.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class CsvColMapperOutput
    {
        public CsvColMapper csvcolmapper { get; set; }
        public List<CsvColMapper> csvcolmapperlist { get; set; }
    }
}
