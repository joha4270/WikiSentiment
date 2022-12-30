using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(WikiSentiment.Startup))]

namespace WikiSentiment
{
    public class Startup : FunctionsStartup
    {

        //Add services here to initialize them before runtime
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //var configuration = builder.GetContext().Configuration;
            //builder.Services.AddAzureAppConfiguration();
           
        }

        //add files to app configuration, later overloads previous one
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "settings.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
    }
}