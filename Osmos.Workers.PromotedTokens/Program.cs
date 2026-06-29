using Microsoft.Extensions.DependencyInjection;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;
using System;

namespace Osmos.Workers.PromotedTokens
{
    class Program
    {
        static void Main()
        {
            ProgramConfigurationBuilder.BuildMain<PromotedTokensApp>(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = ProgramConfigurationBuilder.BuildConfiguration<PromotedTokensApp>(services);

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
            services.Configure<EthSettings>(configuration.GetSection("Eth"));

            services.AddScoped<EthService>();
            services.AddScoped(TokensManager.GetImplementationFactory());
            services.AddScoped(PromotedTokensManager.GetImplementationFactory());

        }
    }
}
