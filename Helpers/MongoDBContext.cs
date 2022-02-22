using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Entities;

namespace WebApi.Helpers
{
    public interface IMongoDBContext
    {
        IMongoCollection<T> GetCollection<T>(string name);
        IMongoQueryable<T> GetQueryable<T>();
        IMongoCollection<T> GetCollection<T>();
        Task Update<T>(T dbObject) where T : BaseModel;
        Task Add<T>(T dbObject) where T : BaseModel;
        void Remove<T>(T dbObject) where T : BaseModel;
    }

    public class MongoDBContext : IMongoDBContext
    {
        private IMongoDatabase _db { get; set; }
        private MongoClient _mongoClient { get; set; }
        public IClientSessionHandle Session { get; set; }

        public MongoDBContext(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MongoDb");
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            settings.ServerApi = new ServerApi(ServerApiVersion.V1);
            _mongoClient = new MongoClient(settings);
            _db = _mongoClient.GetDatabase("menu");
        }

        public async Task Add<T>(T dbObject) where T : BaseModel
        {
            var collection = GetCollection<T>();

            await collection.InsertOneAsync(dbObject);
        }

        public async Task Update<T>(T dbObject) where T : BaseModel
        {
            var collection = GetCollection<T>();

            await collection.ReplaceOneAsync<T>(
                item => item.GetId() == dbObject.GetId(),
                dbObject,
                new UpdateOptions { IsUpsert = true });
        }

        public void Remove<T>(T dbObject) where T : BaseModel
        {
            var collection = GetCollection<T>();

            collection.DeleteOne(x => x._id == dbObject.GetId());
        }

        public IMongoQueryable<T> GetQueryable<T>()
        {
            var collection = _db.GetCollection<T>(typeof(T).Name);
            return collection.AsQueryable<T>();
        }

        public IMongoCollection<T> GetCollection<T>()
        {
            return _db.GetCollection<T>(typeof(T).Name);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _db.GetCollection<T>(name);
        }
    }
}
