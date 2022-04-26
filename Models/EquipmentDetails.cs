using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FHSCAzureFunction.Models
{
    public class EquipmentDetails
    {
        
        public int JobId { get; set; }
        public string Equipment { get; set; }

        public string DescriptionTechnicalObject { get; set; }

        public string EquipmentCategory { get; set; }

        public string TechnicalObjectType { get; set; }

        public string TechnicalIdentificationNo { get; set; }

        public string FunctionalLocation { get; set; }

        public string MaintenancePlant { get; set; }

        public string PlanningPlant { get; set; }

        public string SystemStatus { get; set; }

        public string SapId { get; set; }

        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
