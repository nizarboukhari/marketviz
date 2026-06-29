using Microsoft.Extensions.DependencyInjection;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;
using System;

namespace Osmos.Workers.Notifier
{
    class Program
    {
        static void Main(string[] args)
        {
            ProgramConfigurationBuilder.BuildMain<NotifierApp>(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = ProgramConfigurationBuilder.BuildConfiguration<NotifierApp>(services);

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
            services.Configure<EthSettings>(configuration.GetSection("Eth"));

            services.AddScoped<EthService>();
            services.AddScoped(TxnsManager.GetImplementationFactory());
            services.AddScoped(TokensManager.GetImplementationFactory());
            services.AddScoped(TxnNotificationsManager.GetImplementationFactory());

        }
    }
}
