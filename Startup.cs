using System;
using System.Collections.Generic;
using System.Text;
using FHSCAzureFunction.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FHSCAzureFunction.Services;
using FHSCAzureFunction.Models.Configs;
using Microsoft.Extensions.Hosting;
using FHSCAzureFunction.AppConfig;

[assembly: FunctionsStartup(typeof(FHSCAzureFunction.Startup))]
namespace FHSCAzureFunction
{
    class Startup : FunctionsStartup
    {
        public static int Percentage { get; set; }
        public static string Progress { get; set; }
        
        /*public Startup() { }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }*/
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var Configuration = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
            AppConfiguration.SetConfig(Configuration);
            string ConnString = Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<FlocHierarchyDBContext>(
              options => options.UseSqlServer(ConnString));
            var local_root = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            var azure_root = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

            var actual_root = local_root ?? azure_root;
            var config = new ConfigurationBuilder()
                            .SetBasePath(actual_root)
                            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();
            SDxConfig sDxConfig = new SDxConfig();
            config.Bind("SDxConfig", sDxConfig);
            builder.Services.AddSingleton(sDxConfig);
            builder.Services.AddAutoMapper(typeof(SDxConfig));
            builder.Services.AddSingleton<AuthenticationService>();
            //builder.Services.AddSingleton<AuthenticationService>(serviceProvider => serviceProvider.GetService<AuthenticationService>());
        }
    }
}
