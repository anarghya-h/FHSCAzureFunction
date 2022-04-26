using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models
{
    public class JobCreateViewModel
    {
        [Required(ErrorMessage ="Please enter a name for the job")]
        public string Name { get; set; }

        [Required(ErrorMessage ="Please upload a file")]
        public IFormFile GsapDataCsv { get; set; }

        [Required(ErrorMessage = "Please upload a file")]
        public IFormFile EquipmentDataCsv { get; set; }
    }
}
