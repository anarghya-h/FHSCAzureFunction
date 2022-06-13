using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FHSCAzureFunction.AppConfig
{
    public static class AppConfiguration
    {
        #region Private member variables
        private static IConfiguration currentConfig;
        #endregion

        #region Constructors
        //setting the configuration from Startup
        public static void SetConfig(IConfiguration configuration)
        {
            currentConfig = configuration;
        }
        #endregion

        #region Public members
        //Getting the connection string for the Azure storage account
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

        //Getting the container name from app settings
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
        #endregion

    }
}
