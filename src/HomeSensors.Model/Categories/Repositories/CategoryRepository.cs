using HomeSensors.Model.Categories.Entities;
using HomeSensors.Model.Categories.Models;
using HomeSensors.Model.Data;
using HomeSensors.Model.Helpers;
using HomeSensors.Model.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Categories.Repositories;

public class CategoryRepository(HomeSensorsContext data, HybridCache cache, ITemperatureHubNotifier hubNotifier) : RepositoryBase
{
    public async Task<List<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var categories = await data.Categories
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return categories.ConvertAll(x =>
        {
            return new CategoryResponse
            (
                id: x.Id,
                name: x.Name,
                order: x.Order
            );
        });
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(CategorySaveRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<IFailure>();

        if (request.Name.IsNullOrWhiteSpace())
        {
            failures.Add(new Failure("Category requires a name.", "name"));
        }

        if (failures.Count > 0)
        {
            return Result.Fail<EntityMessage<long>>(failures);
        }

        var category = await data.Categories
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (category is null)
        {
            category = new Category();
            await data.Categories.AddAsync(category, cancellationToken);
        }

        category.Name = request.Name;
        category.Order = request.Order;

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.CategoryAllCacheTag, cancellationToken);
        await hubNotifier.NotifyCurrentReadingsChangedAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Category saved.", category.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var category = await data.Categories
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (category is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Category not found."));
        }

        data.Categories.Remove(category);

        await data.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(CacheHelpers.CategoryAllCacheTag, cancellationToken);
        await hubNotifier.NotifyCurrentReadingsChangedAsync(cancellationToken);

        return Result.Ok(EntityMessage.Create("Category deleted.", category.Id));
    }
}
