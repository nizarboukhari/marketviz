using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Osmos.Core.Data.NoSql.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Osmos.Core.Data.NoSql.Managers
{
    public class MongoDbEntitiesManager<TEntity> : MongoDbBaseManager<TEntity>
        where TEntity : Entity
    {
        public virtual async Task<TEntity> GetOneAsync(string id)
        {
            var entity = await GetOneAsync(e => e.Id == id);
            return entity;
        }

        public override async Task<TEntity> GetOneAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var _predicate = predicate ?? (e => true);

            var entity = await _collection.Find(_predicate).FirstOrDefaultAsync();
            return entity;
        }

        public virtual async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                predicate = _ => true;
            }

            var count = await _collection.CountDocumentsAsync(predicate);

            return count;
        }

        public virtual async Task<GroupCountAggregate[]> GroupCountAsync(string groupField, BsonDocument predicate = null)
        {
            var jsonQuery = @"{
                                $group : {
                                    _id :""$" + groupField + @""",
                                    count: { $sum: 1 }
                                }
                               }  ";

            var groupDocument = BsonSerializer.Deserialize<BsonDocument>(jsonQuery);

            var documents = new List<BsonDocument>();
            if (predicate != null) documents.Add(predicate);
            documents.Add(groupDocument);

            PipelineDefinition<TEntity, GroupCountAggregate> pipeline = documents.ToArray();

            return (await _collection.Aggregate(pipeline).ToListAsync()).ToArray();
        }


        public virtual async Task<dynamic[]> GetManyAsync(PipelineDefinition<TEntity, dynamic> pipeline)
        {
            object[] entities = null;
            using (var cursor = await _collection.AggregateAsync(pipeline))
            {
                entities = cursor.ToList().ToArray();
            }

            return entities;
        }

        public virtual async Task<TEntity[]> GetManyAsync(BsonDocument document)
        {
            var entities = await _collection.Find(document).ToListAsync();

            return entities.ToArray();
        }

        public virtual async Task<dynamic[]> GetManySpecificFieldAsync(BsonDocument document, Expression<Func<TEntity, dynamic>> field = null)
        {
            var entities = await _collection.Find(document).Project(field).ToListAsync();

            return entities.ToArray();
        }

        public override async Task<TEntity[]> GetManyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var _predicate = predicate ?? (e => true);

            var entities = await _collection.Find(_predicate)
                                            .SortByDescending(e => e.CreatedDate)
                                            .ToListAsync();
            return entities.ToArray();
        }

        public virtual async Task<QueryResult<TEntity>> GetManyAsync(QueryOptions<TEntity> queryOptions, Expression<Func<TEntity, bool>> predicate = null)
        {
            var predicates = predicate == null ? null : new Expression<Func<TEntity, bool>>[] {
                predicate
            };

            var result = await GetManyAsync(queryOptions, predicates);
            return result;
        }

        public virtual async Task<QueryResult<TEntity>> GetManyAsync(QueryOptions<TEntity> queryOptions, IEnumerable<Expression<Func<TEntity, bool>>> predicates = null)
        {
            IEnumerable<FilterDefinition<TEntity>> filters = null;
            if (predicates != null && predicates.Any())
            {
                filters = predicates.Select(p => Builders<TEntity>.Filter.Where(p));
            }

            var result = await GetManyAsync(queryOptions, filters);
            return result;
        }

        public virtual async Task<QueryResult<TEntity>> GetManyAsync(QueryOptions<TEntity> queryOptions, IEnumerable<FilterDefinition<TEntity>> filters = null)
        {
            var result = new QueryResult<TEntity>();

            if (queryOptions == null) throw new ArgumentNullException();

            var findOptions = new FindOptions<TEntity, TEntity>();
            if (queryOptions.SortOptions != null && queryOptions.SortOptions.Field != null)
            {
                if (queryOptions.SortOptions.Descending)
                {
                    findOptions.Sort = Builders<TEntity>.Sort.Descending(queryOptions.SortOptions.Field);
                }
                else
                {
                    findOptions.Sort = Builders<TEntity>.Sort.Ascending(queryOptions.SortOptions.Field);
                }
            }

            Expression<Func<TEntity, object>> field = _ => _.CreatedDate;
            if (findOptions.Sort == null)
            {
                findOptions.Sort = Builders<TEntity>.Sort.Descending(field);
            }

            int? size = null;
            int? page = null;
            if (queryOptions.Pagination != null && queryOptions.Pagination.Size > 0)
            {
                size = queryOptions.Pagination.Size;
                page = queryOptions.Pagination.Page > 0 ? queryOptions.Pagination.Page : 1;
            }

            findOptions.Limit = size;
            findOptions.Skip = size * (page - 1);

            var filter = Builders<TEntity>.Filter.Empty;

            if (filters != null && filters.Any())
            {
                filter = Builders<TEntity>.Filter.And(filters);
            }

            var list = new List<TEntity>();

            await Task.WhenAll(new Task[] {
                Task.Run(async () => {
                    result.Total = await _collection.CountDocumentsAsync(filter);
                }),
                Task.Run(async () => {
                    using (var cursor = await _collection.FindAsync(filter, findOptions))
                    {
                        list = await cursor.ToListAsync();
                    }
                })
            });

            result.Entities = list.ToArray();
            result.Pagination = new QueryPaginationOptions
            {
                Page = page,
                Size = size,
                PageCount = size > 0 ? result.Total / size == 0 ? 1 : (Math.Ceiling(decimal.Parse(result.Total.ToString()) / decimal.Parse(size.ToString()))) : 1
            };

            return result;
        }

        public virtual async Task<TEntity[]> GetManyAsync(IEnumerable<FilterDefinition<TEntity>> filters, int? size = null, string lastEntityId = null)
        {
            var filter = Builders<TEntity>.Filter.Empty;


            if (lastEntityId != null)
            {
                var lastEntityFilter = Builders<TEntity>.Filter.And(filters.Concat(new FilterDefinition<TEntity>[] {
                        Builders<TEntity>.Filter.Where(e => e.Id == lastEntityId)
                }));
                var lastEntity = await _collection.Find(lastEntityFilter).FirstOrDefaultAsync();
                if (lastEntity != null)
                {
                    filter = Builders<TEntity>.Filter.Where(e => e.CreatedDate < lastEntity.CreatedDate);
                }
            }

            if (filters != null && filters.Any())
            {
                filter = Builders<TEntity>.Filter.And(filters.Concat(new FilterDefinition<TEntity>[] {
                                filter
                }));
            }

            var findOptions = new FindOptions<TEntity, TEntity>
            {
                Sort = Builders<TEntity>.Sort.Descending(e => e.CreatedDate),
                Limit = size
            };

            var list = new List<TEntity>();
            using (var cursor = await _collection.FindAsync(filter, findOptions))
            {
                list = await cursor.ToListAsync();
            }

            return list.ToArray();
        }

        public virtual async Task<TEntity[]> GetManyAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates, int? size = null, string lastEntityId = null)
        {

            IEnumerable<FilterDefinition<TEntity>> filters = null;
            if (predicates != null && predicates.Any())
            {
                filters = predicates.Select(p => Builders<TEntity>.Filter.Where(p));
            }

            var result = await GetManyAsync(filters, size, lastEntityId);
            return result;
        }

        public virtual async Task<TEntity[]> GetManyAsync(Expression<Func<TEntity, bool>> predicate, int? size = null, string lastEntityId = null)
        {
            var result = await GetManyAsync(predicate == null ? null : new Expression<Func<TEntity, bool>>[] { predicate }, size, lastEntityId);
            return result;
        }

        public virtual async Task<bool> AnyAsync(IEnumerable<FilterDefinition<TEntity>> filters = null)
        {
            var filter = Builders<TEntity>.Filter.Empty;

            if (filters != null && filters.Any())
            {
                filter = Builders<TEntity>.Filter.And(filters);
            }

            bool result = await _collection.Find(filter).AnyAsync();
            return result;
        }

        public virtual async Task<bool> AnyAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates = null)
        {
            IEnumerable<FilterDefinition<TEntity>> filters = null;
            if (predicates != null && predicates.Any())
            {
                filters = predicates.Select(p => Builders<TEntity>.Filter.Where(p));
            }

            var result = await AnyAsync(filters);
            return result;
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var filter = Builders<TEntity>.Filter.Empty;
            if (predicate != null) { 
                filter = Builders<TEntity>.Filter.Where(predicate);
            }

            bool result = await _collection.Find(filter).AnyAsync();
            return result;
        }

        public override async Task<TEntity> CreateOneAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException();

            await _collection.InsertOneAsync(entity);
            return entity;
        }

        public virtual async Task<TEntity> UpdateOneAsync(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException();

            await _collection.FindOneAndReplaceAsync(e => e.Id == entity.Id, entity);

            return entity;
        }

        public virtual async Task<TEntity[]> UpdateManyAsync(TEntity[] entities, UpdateDefinition<TEntity> update)
        {
            if (entities == null) throw new ArgumentNullException();

            if (!entities.Any()) return entities;

            var ids = entities.Select(_ => _.Id).ToArray();

            var filter = Builders<TEntity>.Filter.Where(_ => ids.Contains(_.Id));

            await _collection.UpdateManyAsync(filter, update);

            return entities;
        }

        public virtual async Task<UpdateResult> UpdateManyAsync(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> update)
        {
            var filter = Builders<TEntity>.Filter.Empty;
            if (predicate != null)
            {
                filter = Builders<TEntity>.Filter.Where(predicate);
            }

            var result = await _collection.UpdateManyAsync(filter, update);
            return result;
        }

        public virtual async Task DeleteOneAsync(string id)
        {
            await _collection.FindOneAndDeleteAsync(e => e.Id == id);
        }

        public virtual async Task DeleteManyAsync(Expression<Func<TEntity, bool>> predicate = null)
        {
            var predicates = predicate == null ? null : new Expression<Func<TEntity, bool>>[] {
                predicate
            };

            await DeleteManyAsync(predicates);
        }

        public virtual async Task DeleteManyAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates = null)
        {
            IEnumerable<FilterDefinition<TEntity>> filters = null;
            if (predicates != null && predicates.Any())
            {
                filters = predicates.Select(p => Builders<TEntity>.Filter.Where(p));
            }

            await DeleteManyAsync(filters);
        }

        public virtual async Task DeleteManyAsync(IEnumerable<FilterDefinition<TEntity>> filters = null)
        {
            var filter = Builders<TEntity>.Filter.Empty;

            if (filters != null && filters.Any())
            {
                filter = Builders<TEntity>.Filter.And(filters);
            }

            await _collection.DeleteManyAsync(filter);
        }

        #region internals

        public MongoDbEntitiesManager(IOptions<MongoDbOptions> options, string collectionName) : base(options, collectionName)
        {

            _collection = _collection.Database.GetCollection<TEntity>(collectionName);
        }

        #endregion
    }

    public class GroupCountAggregate
    {
        [BsonId]
        public string Id { get; set; }

        public long Count { get; set; }
    }

}
