using System;
using System.Collections.Generic;
using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace FHSCAzureFunction.Models
{
    public class Floc4
    {
        public string SuperiorFunctionalLocation { get; set; }
        public int JobId { get; set; }
        public string FunctionalLocation { get; set; }
        public string DescriptionFunctionLocation { get; set; }
        public string TechnicalObjectType { get; set; }
        public string MaintenancePlant { get; set; }
        public string PlanningPlant { get; set; }
        public string SortField { get; set; }
        public string FunctionalLocationCategory { get; set; }
        public string SystemStatus { get; set; }
        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
