using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace FHSCAzureFunction.Models
{
    public class Floc2
    {
        
        public int JobId { get; set; }
        public string FlocLevel2Name { get; set; }
        public string FlocLevel2Description { get; set; }
        public string TerminalCode { get; set; }

        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
