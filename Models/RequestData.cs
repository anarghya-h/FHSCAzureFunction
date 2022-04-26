using System;
using System.Collections.Generic;
using System.Text;

namespace FHSCAzureFunction.Models
{
    public class RequestData
    {
        public int JobId { get; set; }
        public string GsapFilePath { get; set; }
        public string EquipmentFilePath { get; set; }
        public string ServerUri { get; set; }
        public string AccessToken { get; set; }
    }
}
