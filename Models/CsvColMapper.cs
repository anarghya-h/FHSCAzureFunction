using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class CsvColMapper
    {
        [Key]
        public int ColMapperId { get; set; }
        public string DbColName { get; set; }
        [Required(ErrorMessage = "This field is required")]
        public string CsvColName { get; set; }
        [Required(ErrorMessage = "This field is required")]
        public int CsvColSequence { get; set; }
        public string CsvName { get; set; }
        public string DbTableName { get; set; }
    }
}
