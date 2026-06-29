using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Business.Worker.Services;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.IO;

namespace Osmos.Business.Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            _ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var appService = serviceProvider.GetService<App>();

            appService.Run();
        }

        private static void _ConfigureServices(IServiceCollection services)
        {
            var loggerFactory = new LoggerFactory();
            services.AddSingleton(loggerFactory);
            services.AddLogging();

            _ = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false);

            //if (environmentName != null)
            //{
            //    builder.AddJsonFile($"appsettings.{environmentName}.json", true);
            //}

            //Console.WriteLine($"environmentName : {environmentName}");

            var configuration = builder.Build();

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));

            services.AddScoped(TokensManager.GetImplementationFactory());
            services.AddScoped(HolderInfosManager.GetImplementationFactory());
            services.AddScoped(TxnsManager.GetImplementationFactory());
            services.AddScoped(RawTransactionsManager.GetImplementationFactory());
            services.AddScoped(IgnoredTokensManager.GetImplementationFactory());
            services.AddScoped(TelegramScoreMessagesManager.GetImplementationFactory());
            services.AddScoped(TelegramOpportunityMessagesManager.GetImplementationFactory());
            services.AddScoped(HotTokensManager.GetImplementationFactory());

            services.AddScoped<FunctionSignaturesTable>();

            services.AddTransient<App>();

        }
    }
}
