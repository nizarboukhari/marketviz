using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Osmos.Workers.Common
{
    public class ProgramConfigurationBuilder
    {
        public static IConfigurationRoot BuildConfiguration<TApp>(IServiceCollection services)
        where TApp : BaseApp
        {

            var loggerFactory = new LoggerFactory();
            services.AddSingleton(loggerFactory);
            services.AddLogging();

            _ = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false);

            var configuration = builder.Build();

            services.AddOptions();
            services.AddTransient<TApp>();

            return configuration;
        }

        public static void BuildMain<TApp>(Action<IServiceCollection> configureServices)
            where TApp: BaseApp
        {
            var serviceCollection = new ServiceCollection();
            configureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var appService = serviceProvider.GetService<TApp>();

            appService.Run();
        }
    }

    public abstract class BaseApp{
        public void Run() {
            RunAsync().Wait();
        }
        protected virtual async Task RunAsync()
        {
            await Task.FromResult(0);
        }
    }

}
