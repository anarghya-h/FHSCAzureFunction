using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace FHSCAzureFunction.Models
{
    public class Floc1
    {
        
        public int JobId { get; set; }
        public string TerminalCode { get; set; }
        public string TerminalDescription { get; set; }
        public string Country { get; set; }
        public string Cluster { get; set; }
        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
