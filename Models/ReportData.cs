using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class ReportData
    {
        public string JobID { get; set; }
        public bool isFlocErrorSelected { get; set; }
        public bool isFloc1Selected { get; set; }
        public bool isFloc2Selected { get; set; }
        public bool isFloc3Selected { get; set; }
        public bool isFloc4Selected { get; set; }
        public bool isEquipmentErrorSelected { get; set; }
        public bool isEquipmentSelected { get; set; }
    }
}
