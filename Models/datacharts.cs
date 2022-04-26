using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class Datacharts
    {
        [Key]
        public int Id { get; set; }
        public string PbsType { get; set; }
        public int TotalRecords { get; set; }        
        public int Errors { get; set; }
        public int NewRecords { get; set; }
        public int ImpactedRecords { get; set; }
        [Column(TypeName = "decimal(18,5)")]
        public decimal ErrorPercentage { get; set; }
        [Column(TypeName = "decimal(18,5)")]
        public decimal ImpactedPercentage { get; set; }
        [Column(TypeName = "decimal(18,5)")]
        public decimal NewRecordPercentage { get; set; }
        public int JobId { get; set; }
        [ForeignKey("JobId")]
        public virtual JobDetails JobDetails { get; set; }
    }
}
