using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace FHSCAzureFunction.Models
{
    public class JobDetails
    {
        [Key]
        public int JobId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime Date { get; set; }
        public string ErrorMessage { get; set; }
        public string TimeTaken { get; set; }


    }
}
