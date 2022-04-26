using System;
using System.Collections.Generic;
using System.Text;

namespace FHSCAzureFunction.Models
{
    public class JobOutput
    {
        public JobCreateViewModel jobcreate { get; set; }
        public List<JobDetails> joblist { get; set; }
        public ReportData reportData { get; set; }
    }
}
