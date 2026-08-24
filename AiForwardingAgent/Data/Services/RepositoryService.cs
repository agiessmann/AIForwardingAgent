using System.Linq.Expressions;
using AiForwardingAgent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AiForwardingAgent.Data.Services;

public class RepositoryService<TEntity>(ILogger<RepositoryService<TEntity>> logger, IDbContextFactory<ApplicationDbContext> dbContext) : IRepositoryService<TEntity> where TEntity : class, new()
{
    
    public async Task<TEntity> Create(TEntity entity)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbCreate(ctx, entity);
    }

    public async Task<TEntity> DbCreate(DbContext ctx, TEntity entity)
    {
        try
        {
            CheckDbContextIsNull(ctx, "create");
            CheckKnownEntity(ctx, entity, "create");

            await ctx.Set<TEntity>().AddAsync(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create entity");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task CreateRange(IEnumerable<TEntity> entities)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        await DbCreateRange(ctx, entities);
    }

    public async Task DbCreateRange(DbContext ctx, IEnumerable<TEntity> entities)
    {
        try
        {
            CheckDbContextIsNull(ctx, "create range");

            await ctx.Set<TEntity>().AddRangeAsync(entities);
            await ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create range of entities");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task<bool> Delete(TEntity entity)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbDelete(ctx, entity);
    }

    public async Task<bool> DbDelete(DbContext ctx, TEntity entity)
    {
        try
        {
            CheckDbContextIsNull(ctx, "delete");
            CheckKnownEntity(ctx, entity, "delete");

            ctx.Set<TEntity>().Remove(entity);
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete entity");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task<bool> DeleteByExpression(Expression<Func<TEntity, bool>> predicate)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbDeleteByExpression(ctx, predicate);
    }

    public async Task<bool> DbDeleteByExpression(DbContext ctx, Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            CheckDbContextIsNull(ctx, "delete by expression");

            var entities = await ctx.Set<TEntity>().Where(predicate).ToListAsync();
            if (entities.Count == 0)
            {
                return false;
            }

            foreach (var entity in entities)
            {
                await DbDelete(ctx, entity);
            }

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete by expression");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task<IEnumerable<TEntity>> GetAll()
    {
        try
        {
            await using var ctx = await dbContext.CreateDbContextAsync();
            return await DbGetAll(ctx);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get all entities");
            throw;
        }
    }

    

    public async Task<IEnumerable<TEntity>> DbGetAll(DbContext ctx)
    {
        return await ctx.Set<TEntity>().AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<TEntity>> GetAllByExpression(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            await using var ctx = await dbContext.CreateDbContextAsync();
            return await DbGetAllByExpression(ctx, predicate);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to get all by expression");
            throw;
        }
    }

    public async Task<IEnumerable<TEntity>> DbGetAllByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate)
    {
        if (predicate == null) return Enumerable.Empty<TEntity>();
        return await ctx.Set<TEntity>().Where(predicate).AsNoTracking().ToListAsync();
    }

    public async Task<TEntity?> GetByExpression(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            await using var ctx = await dbContext.CreateDbContextAsync();
            return await DbGetByExpression(ctx, predicate);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to get by expression");
            throw;
        }
    }

    public async Task<TEntity?> GetById(int id)
    {
        try
        {
            await using var ctx = await dbContext.CreateDbContextAsync();
            return await DbGetById(ctx, id);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to get by id");
            throw;
        }
    }
    
    public async Task<TEntity?> DbGetById(DbContext ctx, int id)
    {
        if (id == 0) return null;
        return await ctx.Set<TEntity>().FindAsync(id);
    }

    public async Task<TEntity?> DbGetByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate)
    {
        if (predicate == null) return null;
        return await ctx.Set<TEntity>().FirstOrDefaultAsync(predicate);
    }

    public async Task<TEntity> Update(TEntity entity)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbUpdate(ctx, entity);
    }

    public async Task<TEntity> DbUpdate(DbContext ctx, TEntity entity)
    {
        try
        {
            CheckDbContextIsNull(ctx, "update");
            CheckKnownEntity(ctx, entity, "update");

            ctx.Set<TEntity>().Update(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException e)
        {
            logger?.LogError(e, "Failed to update entity");
            throw new RepositoryServiceException(message: "database update error", innerException: e);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to update entity");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task<int> GetAllCount()
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbGetAllCount(ctx);
    }

    public async Task<int> DbGetAllCount(DbContext ctx)
    {
        return await ctx.Set<TEntity>().CountAsync();
    }

    public async Task<int> GetCountByExpression(Expression<Func<TEntity, bool>> predicate)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbGetCountByExpression(ctx, predicate);
    }

    public async Task<int> DbGetCountByExpression(DbContext ctx, Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null) return 0;
        return await ctx.Set<TEntity>().CountAsync(predicate);
    }

    public async Task<int> GetSumByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> selector)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbGetSumByExpression(ctx, predicate, selector);
    }

    public async Task<int> DbGetSumByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, int>> selector)
    {
        if (predicate == null) return 0;
        return await ctx.Set<TEntity>().Where(predicate).SumAsync(selector);
    }

    public async Task<int> GetMaxByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, int>> selector)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbGetMaxByExpression(ctx, predicate, selector);
    }

    public async Task<int> DbGetMaxByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, int>> selector)
    {
        if (predicate == null) return 0;
        return await ctx.Set<TEntity>().Where(predicate).MaxAsync(selector);
    }

    public async Task<IEnumerable<string>> GetSelectedListOfStringDistinctByExpression(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, string>> selector)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        return await DbGetSelectedListOfStringDistinctByExpression(ctx, predicate, selector);
    }

    public async Task UpdateRange(IEnumerable<TEntity> entities)
    {
        await using var ctx = await dbContext.CreateDbContextAsync();
        await DbUpdateRange(ctx, entities);
    }

    public async Task DbUpdateRange(DbContext ctx, IEnumerable<TEntity> entities)
    {
        try
        {
            CheckDbContextIsNull(ctx, "update range");

            ctx.Set<TEntity>().UpdateRange(entities);
            await ctx.SaveChangesAsync();
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Failed to update range of entities");
            throw new RepositoryServiceException(message: "database error", innerException: e);
        }
    }

    public async Task<IEnumerable<string>> DbGetSelectedListOfStringDistinctByExpression(DbContext ctx, Expression<Func<TEntity, bool>>? predicate, Expression<Func<TEntity, string>> selector)
    {
        if (predicate == null) return Enumerable.Empty<string>();
        return await ctx.Set<TEntity>().Where(predicate).Select(selector).Distinct().ToListAsync();
    }

    public void CheckKnownEntity(DbContext ctx, TEntity entity, string dbOperation)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(entity);

        var entityType = ctx.Model.FindEntityType(typeof(TEntity))
                         ?? ctx.Model.FindEntityType(entity.GetType());

        if (entityType != null)
        {
            return;
        }

        logger.LogError(
            "Failed to {DbOperation}. Entity type {EntityType} is not configured in DbContext {DbContextType}",
            dbOperation,
            typeof(TEntity).FullName,
            ctx.GetType().FullName);

        throw new RepositoryServiceException($"Entity type '{typeof(TEntity).Name}' is not configured in the DbContext.");
    }

    public void CheckDbContextIsNull(IAsyncDisposable? ctx, string dbOperation)
    {
        if (ctx != null) return;
        logger?.LogError("Failed to {DbOperation} Entity, DB Context is null", dbOperation);
        throw new RepositoryServiceException(message: "DB Context is Null");
    }
}