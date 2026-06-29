using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Osmos.Core.Data.NoSql.Managers
{
    public class MongoDbBaseManager<TEntity>
       where TEntity : class
    {
        #region internals

        private MongoDbOptions _options;
        protected IMongoCollection<TEntity> _collection = null;

        public MongoDbBaseManager(IOptions<MongoDbOptions> options, string collectionName)
        {
            _options = options.Value;

            var settings = MongoClientSettings.FromUrl(new MongoUrl(_options.Url));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            settings.ConnectTimeout = TimeSpan.FromSeconds(60);
            settings.SocketTimeout = TimeSpan.FromMinutes(5);
            settings.MaxConnectionIdleTime = TimeSpan.FromSeconds(30);
            void SocketConfigurator(Socket s) => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            settings.ClusterConfigurator = builder => {
                builder
                .ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>)SocketConfigurator));
                builder.Subscribe<CommandStartedEvent>(e => {
                    // log query
                });
            };
            var conventionPack = new ConventionPack {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("ConvetionPack", conventionPack, type => true);
            var mongoClient = new MongoClient(settings);

            var database = mongoClient.GetDatabase(_options.DbName);

            _collection = database.GetCollection<TEntity>(collectionName);
        }

        public virtual async Task<TEntity> GetOneAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var _predicate = predicate ?? (e => true);

            var entity = await _collection.Find(_predicate).FirstOrDefaultAsync();
            return entity;
        }

        public virtual async Task<TEntity[]> GetManyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var _predicate = predicate ?? (e => true);

            var entities = await _collection.Find(_predicate)
                                            .ToListAsync();
            return entities.ToArray();
        }

        public virtual async Task<TEntity> CreateOneAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException();

            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task<TEntity[]> CreateManyAsync(TEntity[] entities)
        {
            if (entities == null || !entities.Any()) throw new ArgumentNullException();

            await _collection.InsertManyAsync(entities);
            return entities;
        }

        public virtual async Task<TEntity> UpdateOneAsync(Expression<Func<TEntity, bool>> predicate, TEntity entity)
        {
            if (predicate == null || entity == null) throw new ArgumentNullException();

            await _collection.FindOneAndReplaceAsync(predicate, entity);

            return entity;
        }

        public virtual async Task DeleteOneAsync(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException();

            await _collection.FindOneAndDeleteAsync(predicate);
        }

        public async Task WatchAsync(Action<ChangeStreamDocument<TEntity>> processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));

            using (var cursor = await _collection.WatchAsync())
            {
                await cursor.ForEachAsync(processor);
            }
        }

        public static Func<IServiceProvider, TManager> GetImplementationFactory<TManager>(string collectionName)
            where TManager : MongoDbBaseManager<TEntity>
        {
            TManager implementationFactory(IServiceProvider provider) =>
                Activator.CreateInstance(typeof(TManager), new object[] { provider.GetService<IOptions<MongoDbOptions>>(), collectionName }) as TManager;
            return implementationFactory;
        }

        #endregion
    }

}
