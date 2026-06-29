using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Osmos.Core.Business.Data.NoSql.Managers
{
    public abstract class DocumentsManager
    {
        private MongoDbOptions _options = null;
        protected GridFSBucket _bucket = null;

        public DocumentsManager(IOptions<MongoDbOptions> options, string name)
        {
            _options = options.Value;

            var settings = MongoClientSettings.FromUrl(new MongoUrl(_options.Url));
            settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            settings.ConnectTimeout = TimeSpan.FromSeconds(60);
            settings.SocketTimeout = TimeSpan.FromMinutes(5);
            settings.MaxConnectionIdleTime = TimeSpan.FromSeconds(30);
            void SocketConfigurator(Socket s) => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            settings.ClusterConfigurator = builder => builder
                .ConfigureTcp(tcp => tcp.With(socketConfigurator: (Action<Socket>)SocketConfigurator));
            var conventionPack = new ConventionPack {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("ConvetionPack", conventionPack, type => true);
            var mongoClient = new MongoClient(settings);

            var database = mongoClient.GetDatabase(_options.DbName);

            _bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = name,
                WriteConcern = WriteConcern.Acknowledged
            });
        }

        public async Task<string> UploadAsync(string fileName, MemoryStream stream)
        {

            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string contentType);

            if (contentType == null) contentType = "application/octet-stream";

            var options = new GridFSUploadOptions()
            {
                Metadata = new BsonDocument { { "content-type", contentType } }
            };

            var result = await _bucket.UploadFromStreamAsync(fileName, stream, options);

            stream.Dispose();

            return result.ToString();
        }

        public virtual async Task<string> UploadAsync(string fileName, MemoryStream stream, List<BsonElement> metadatas = null)
        {

            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string contentType);

            if (contentType == null) contentType = "application/octet-stream";

            var options = new GridFSUploadOptions()
            {
                Metadata = new BsonDocument { { "content-type", contentType } }
            };

            if (metadatas != null)
            {
                foreach (var metadata in metadatas)
                {
                    options.Metadata.Add(metadata);
                }
            }

            var result = await _bucket.UploadFromStreamAsync(fileName, stream, options);

            stream.Dispose();

            return result.ToString();
        }

        public async Task<GridFSFileInfo[]> ListAsync()
        {
            var filter = Builders<GridFSFileInfo>.Filter.Where(_ => true);
            var sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime);
            var options = new GridFSFindOptions
            {
                //Limit = 1,
                Sort = sort
            };

            var fileInfos = new List<GridFSFileInfo>();
            using (var cursor = _bucket.Find(filter, options))
            {
                fileInfos = await cursor.ToListAsync();
            }

            return fileInfos.ToArray();
        }

        public async Task<GridFSFileInfo[]> ListAsync(List<string> documentNames)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Where(_ => documentNames.Contains(_.Filename));
            var sort = Builders<GridFSFileInfo>.Sort.Descending(x => x.UploadDateTime);
            var options = new GridFSFindOptions
            {
                //Limit = 1,
                Sort = sort
            };

            var fileInfos = new List<GridFSFileInfo>();
            using (var cursor = _bucket.Find(filter, options))
            {
                fileInfos = await cursor.ToListAsync();
            }

            return fileInfos.ToArray();
        }

        public async Task<GridFSFileInfo> FindAsync(string filename)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Where(_ => _.Filename == filename);

            var fileInfos = new List<GridFSFileInfo>();

            using (var cursor = _bucket.Find(filter))
            {
                fileInfos = await cursor.ToListAsync();
            }

            if (fileInfos.Any())
            {
                return fileInfos.First();
            }
            else
            {
                return null;
            }

        }

        public async Task<GridFSFileInfo> FindByIdAsync(string id)
        {
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", new ObjectId(id));

            var fileInfos = new List<GridFSFileInfo>();

            using (var cursor = _bucket.Find(filter))
            {
                fileInfos = await cursor.ToListAsync();
            }

            if (fileInfos.Any())
            {
                return fileInfos.First();
            }
            else
            {
                return null;
            }

        }

        public async Task<byte[]> DownloadBytesAsync(string filename)
        {
            byte[] file;

            try
            {
                file = await _bucket.DownloadAsBytesByNameAsync(filename);
            }
            catch (GridFSFileNotFoundException)
            {
                return null;
            }

            return file;
        }

        public async Task DeleteAsync(ObjectId id)
        {
            await _bucket.DeleteAsync(id);
        }

    }
}
