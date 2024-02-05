using DSTV3.Common.BO.Entities;
using DSTV3.UploadInterface.Api.Entities;
using DSTV3.UploadInterface.Api.Models.MongoDbModels;
using Gridify;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;


namespace DSTV3.UploadInterface.Api.MongoGenericRepository
{

    public class MongoRepository<TEntity> : IMongoRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly IMongoCollection<TEntity> _collection;

        public MongoRepository(IMongoDbSettings settings)
        {
            var database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
            _collection = database.GetCollection<TEntity>(GetCollectionName(typeof(TEntity)));
        }


        private protected string GetCollectionName(Type documentType)
        {
            var Collection = documentType.Name;
            return Collection;
        }


        public virtual List<TEntity> List(GridifyQuery model)
        {
            if (model.Page == -1)
                return _collection.AsQueryable().Where(c =>c.Status == true).OrderByDescending(c => c.InsertDateTime).ToList();

            else
                return _collection.AsQueryable().Gridify(model).Data.OrderByDescending(c => c.InsertDateTime).ToList();
        }


        public virtual TEntity FindOne(Expression<Func<TEntity, bool>> filterExpression)
        {
            return _collection.Find(filterExpression).FirstOrDefault();
        }


        public virtual TEntity InsertOne(TEntity document)
        {
            _collection.InsertOne(document);
            return document;
        }


        public List<TEntity> InsertMany(List<TEntity> documents)
        {
            InsertManyOptions insertManyOptions = new InsertManyOptions { IsOrdered = true };
            _collection.InsertMany(documents, insertManyOptions);
            return documents;
        }

        public TEntity ReplaceOne(TEntity document)
        {
            var filter = Builders<TEntity>.Filter.Eq(doc => doc.Id, document.Id);
            _collection.FindOneAndReplace(filter, document);
            return document;
        }

        public void DeleteOne(Expression<Func<TEntity, bool>> filterExpression)
        {
            _collection.FindOneAndDelete(filterExpression);
        }

        public void DeleteMany(Expression<Func<TEntity, bool>> filterExpression)
        {
            DeleteOptions deleteOptions = new DeleteOptions();
            deleteOptions.Hint = true;
            _collection.DeleteMany(filterExpression, deleteOptions);
        }

        public int GetCount(GridifyQuery model)
        {
            var count = _collection.AsQueryable().Gridify(model).Data.Count();
            int castCount = (int)count;
            return castCount;
        }

        public bool Exists(Expression<Func<TEntity, bool>> filterExpression)
        {
            return _collection.AsQueryable().Any(filterExpression);
        }

        public List<TEntity> All(Expression<Func<TEntity, bool>> filterExpression,int page = 1,int pagesize = 20)
        {
            var skip = (page - 1) * pagesize;
            return _collection.Find(filterExpression).Skip(skip).Limit(pagesize).ToList();
        }

        public List<TEntity> Search(Expression<Func<TEntity, bool>> filterExpression, GridifyQuery model)
        {
            return _collection.AsQueryable().Where(filterExpression).Gridify(model).Data.ToList();
        }

        public long SumTotal(Expression<Func<TEntity, long>> filterExpressionsum)
        {
            var SumItem = _collection.AsQueryable().Sum(filterExpressionsum);
            return SumItem;
        }

        public int CountTotal()
        {
            var count = _collection.AsQueryable().Count();
            int castCount = (int)count;
            return castCount;
        }

        public List<DFile> FindDuplicateDocuments()
        {
            var pipeline = new[]
            {
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument("FileId", "$FileId") },
                    { "count", new BsonDocument("$sum", 1) },
                    { "docs", new BsonDocument("$push", "$$ROOT") }
                }),
                new BsonDocument("$match", new BsonDocument
                {
                    { "count", new BsonDocument("$gt", 1) }
                }),
                new BsonDocument("$unwind", "$docs"),
                new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$docs"))
        };
            var result = _collection.Aggregate<DFile>(pipeline).ToList();
            return result;
        }
    }

}
