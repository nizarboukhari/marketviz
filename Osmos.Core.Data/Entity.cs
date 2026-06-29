using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Osmos.Core.Data
{
    public abstract class Entity
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime CreatedDate { get; set; }

        public Entity()
        {
            Id = Guid.NewGuid().ToString();
            CreatedDate = DateTime.UtcNow;
        }
    }

    public abstract class NamedEntity : Entity
    {
        public string Name { get; set; }
    }

    public abstract class RankedEntity : NamedEntity {
        public int Rank { get; set; }

        public RankedEntity()
        {
            Rank = -1;
        }
    }

    public abstract class EntityLookup
    {
        [BsonId]
        public string Id { get; set; }
    }

    public abstract class NamedEntityLookup : EntityLookup
    {
        public string Name { get; set; }
    }

    public abstract class RankedEntityLookup : NamedEntityLookup
    {
        public int Rank { get; set; }

        public RankedEntityLookup()
        {
            Rank = 9999999;
        }
    }
}
