using System.Linq.Expressions;
using AiForwardingAgent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AiForwardingAgent.Data.Services;

public interface IRepositoryService<TEntity> where TEntity : class, new()
{
    Task<TEntity> Create(TEntity entity);
    Task<TEntity> DbCreate(DbContext ctx, TEntity entity);
    Task CreateRange(IEnumerable<TEntity> entities);
    Task DbCreateRange(DbContext ctx, IEnumerable<TEntity> entities);
    Task<bool> Delete(TEntity entity);
    Task<bool> DbDelete(DbContext ctx, TEntity entity);
    Task<bool> DeleteByExpression(Expression<Func<TEntity, bool>> predicate);
    Task<bool> DbDeleteByExpression(DbContext ctx, Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> GetAll();
    Task<IEnumerable<TEntity>> DbGetAll(DbContext ctx);
    Task<IEnumerable<TEntity>> GetAllByExpression(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> DbGetAllByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate);
    Task<TEntity?> GetByExpression(Expression<Func<TEntity, bool>> predicate);
    
    Task<TEntity?> GetById(int id);
    Task<TEntity?> DbGetById(DbContext ctx, int id);
    Task<TEntity?> DbGetByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate);
    Task<TEntity> Update(TEntity entity);
    Task<TEntity> DbUpdate(DbContext ctx, TEntity entity);
    Task<int> GetAllCount();
    Task<int> DbGetAllCount(DbContext ctx);
    Task<int> GetCountByExpression(Expression<Func<TEntity, bool>> predicate);
    Task<int> DbGetCountByExpression(DbContext ctx, Expression<Func<TEntity, bool>> predicate);
    Task<int> GetSumByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> selector);
    Task<int> DbGetSumByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, int>> selector);
    Task<int> GetMaxByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> selector);
    Task<int> DbGetMaxByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, int>> selector);
    Task<IEnumerable<string>> GetSelectedListOfStringDistinctByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> selector);
    Task UpdateRange(IEnumerable<TEntity> entities);
    Task DbUpdateRange(DbContext ctx, IEnumerable<TEntity> entities);
    Task<IEnumerable<string>> DbGetSelectedListOfStringDistinctByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, string>> selector);
    void CheckKnownEntity(DbContext ctx, TEntity entity, string dbOperation);
    void CheckDbContextIsNull(IAsyncDisposable? ctx, string dbOperation);
}