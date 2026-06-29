using Microsoft.Extensions.DependencyInjection;
using Osmos.Business.Data.NoSql.Managers;
using Osmos.Core.Data.NoSql.Models;
using Osmos.Workers.Common;

namespace Osmos.Workers.Messenger
{
    class Program
    {
        static void Main()
        {
            ProgramConfigurationBuilder.BuildMain<MessengerApp>(ConfigureServices);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var configuration = ProgramConfigurationBuilder.BuildConfiguration<MessengerApp>(services);

            services.AddOptions();

            services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
            services.Configure<TelegramSettings>(configuration.GetSection("Telegram"));

            services.AddScoped(TxnsManager.GetImplementationFactory());
            services.AddScoped(TokensManager.GetImplementationFactory());
            services.AddScoped(HotTokensManager.GetImplementationFactory());
            services.AddScoped(TelegramScoreMessagesManager.GetImplementationFactory());
            services.AddScoped(TelegramOpportunityMessagesManager.GetImplementationFactory());
            services.AddScoped(TelegramHotMessagesManager.GetImplementationFactory());

            services.AddScoped<TelegramManager>();

        }
    }
}
