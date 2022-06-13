using FHSCAzureFunction.AppConfig;
using FHSCAzureFunction.Models;
using FHSCAzureFunction.Models.Configs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Serilog;
using System;

[assembly: FunctionsStartup(typeof(FHSCAzureFunction.Startup))]
namespace FHSCAzureFunction
{
    class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //Obtaining the configuration
            var Configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            AppConfiguration.SetConfig(Configuration);
            //Reading the connection string for the database
            string ConnString = Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<FlocHierarchyDBContext>(
              options => options.UseSqlServer(ConnString));

            //Getting the base path
            var local_root = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azure_root = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

            var actual_root = local_root ?? azure_root;
            //Creating a config to retrieve SDx Config data
            var config = new ConfigurationBuilder()
                            .SetBasePath(actual_root)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();

            SDxConfig sDxConfig = new SDxConfig();
            config.Bind("SDxConfig", sDxConfig);
            builder.Services.AddSingleton(sDxConfig);
            builder.Services.AddAutoMapper(typeof(SDxConfig));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.AzureTableStorageWithProperties(CloudStorageAccount.Parse(AppConfiguration.GetConfiguration("StorageAccount")), Serilog.Events.LogEventLevel.Information, storageTableName: "FHSCLogs", propertyColumns: new string[] { "JobId", "JobName" })
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Services.AddSingleton(Log.Logger);
            builder.Services.AddLogging(log =>
            {
                log.AddSerilog(Log.Logger);
            });
        }
    }
}
