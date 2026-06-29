using Microsoft.Extensions.DependencyInjection;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;

namespace Osmos.Workers.PendingTxns
{
    class Program
    {
        static void Main()
        {
            ProgramConfigurationBuilder.BuildMain<PendingTxnsApp>(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = ProgramConfigurationBuilder.BuildConfiguration<PendingTxnsApp>(services);

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
            services.Configure<EthSettings>(configuration.GetSection("Eth"));
            services.Configure<FunctionSignatureSettings>(configuration.GetSection("FunctionSignature"));

            services.AddScoped<EthService>();
            services.AddScoped(TxnsManager.GetImplementationFactory());

        }
    }
}
