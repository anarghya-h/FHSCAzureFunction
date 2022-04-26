using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FHSCAzureFunction.AppConfig
{
    public static class AppConfiguration
    {
        private static IConfiguration currentConfig;

        public static void SetConfig(IConfiguration configuration)
        {
            currentConfig = configuration;
        }


        public static string GetConfiguration(string configKey)
        {
            try
            {
                string connectionString = currentConfig.GetConnectionString(configKey);
                return connectionString;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

        public static string GetContainer(string configKey)
        {
            try
            {
                string container = Environment.GetEnvironmentVariable("Container");
                return container;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }

    }
}
