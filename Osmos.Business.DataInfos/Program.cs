using Microsoft.Extensions.DependencyInjection;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Common;
using Osmos.Workers.Helpers.Eth;
using Osmos.Workers.Helpers.Eth.Models;

namespace Osmos.Business.DataInfos
{
    class Program
    {
        static void Main()
        {
            ProgramConfigurationBuilder.BuildMain<DataInfosApp>(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = ProgramConfigurationBuilder.BuildConfiguration<DataInfosApp>(services);

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
            services.Configure<EthSettings>(configuration.GetSection("Eth"));

            services.AddScoped<EthService>();
            services.AddScoped(DataInfosManager.GetImplementationFactory());

        }
    }
}
