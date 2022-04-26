using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class TechnicalObjects
    {
        
        public int JobId { get; set; }
        [Key]
        public string TechnicalObjectType { get; set; }
        public string Description { get; set; }

        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
