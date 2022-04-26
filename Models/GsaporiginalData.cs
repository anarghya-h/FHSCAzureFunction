using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FHSCAzureFunction.Models
{
    public class GsaporiginalData
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
        public bool IsNewRecord { get; set; }
        public bool HasError { get; set; }
        public int ActualLevel { get; set; }
        public bool SuperiorFlocHasError { get; set; }
        public string ErrorMessage { get; set; }

        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
