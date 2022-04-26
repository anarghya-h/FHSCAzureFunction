using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FHSCAzureFunction.Models.Configs
{
    public class SDxConfig
    {
        public string ServerBaseUri { get; set; }
        public string WebClientBaseUri { get; set; }
        public string ServerResourceID { get; set; }
        public string AuthServerAuthority { get; set; }
        public string AuthClientId { get; set; }
        public string AuthClientSecret { get; set; }
    }
}

