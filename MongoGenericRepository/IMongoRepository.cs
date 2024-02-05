using DSTV3.Common.BO.Entities;
using DSTV3.UploadInterface.Api.Entities;
using Gridify;
using System.Linq.Expressions;

namespace DSTV3.UploadInterface.Api.MongoGenericRepository
{
    public interface IMongoRepository<TEntity> where TEntity : BaseEntity
    {
        List<TEntity> Search(Expression<Func<TEntity, bool>> filterExpression, GridifyQuery model);
        List<TEntity> All(Expression<Func<TEntity, bool>> filterExpression,int page = 1, int pagesize = 20);
        List<TEntity> List(GridifyQuery model);
        int GetCount(GridifyQuery model);
        TEntity FindOne(Expression<Func<TEntity, bool>> filterExpression);
        bool Exists(Expression<Func<TEntity, bool>> filterExpression);
        TEntity InsertOne(TEntity document);
        List<TEntity> InsertMany(List<TEntity> documents);
        TEntity ReplaceOne(TEntity document);
        void DeleteOne(Expression<Func<TEntity, bool>> filterExpression);
        void DeleteMany(Expression<Func<TEntity, bool>> filterExpression);
        long SumTotal(Expression<Func<TEntity, long>> filterExpressionsum);
        int CountTotal();
        List<DFile> FindDuplicateDocuments();
    }
}
