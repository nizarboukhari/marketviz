using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Osmos.Core.Data.NoSql.Entities;
using Osmos.Core.Data.NoSql.Models;
using System;


namespace Osmos.Core.Data.NoSql.Managers
{
    public class SmsMessagesManager : MongoDbEntitiesManager<SmsMessage>
    {
        public static Func<IServiceProvider, SmsMessagesManager> GetImplementationFactory()
        {
            SmsMessagesManager implementationFactory(IServiceProvider provider) =>
                new SmsMessagesManager(provider.GetService<IOptions<MongoDbOptions>>(), "sms-messages");
            return implementationFactory;
        }

        public SmsMessagesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {
        }
    }
}
