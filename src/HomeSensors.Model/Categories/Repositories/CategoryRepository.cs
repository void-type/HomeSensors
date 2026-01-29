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

public class CategoryRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;
    private readonly HybridCache _cache;
    private readonly ITemperatureHubNotifier _hubNotifier;

    public CategoryRepository(HomeSensorsContext data, HybridCache cache, ITemperatureHubNotifier hubNotifier)
    {
        _data = data;
        _cache = cache;
        _hubNotifier = hubNotifier;
    }

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var data = await _data.Categories
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        return data.ConvertAll(x =>
        {
            return new CategoryResponse
            (
                id: x.Id,
                name: x.Name,
                order: x.Order
            );
        });
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(CategorySaveRequest request)
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

        var category = await _data.Categories
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (category is null)
        {
            category = new Category();
            _data.Categories.Add(category);
        }

        category.Name = request.Name;
        category.Order = request.Order;

        await _data.SaveChangesAsync();

        await _cache.RemoveByTagAsync(CacheHelpers.CategoryAllCacheTag);
        await _hubNotifier.NotifyCurrentReadingsChangedAsync();

        return Result.Ok(EntityMessage.Create("Category saved.", category.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(int id)
    {
        var category = await _data.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Category not found."));
        }

        _data.Categories.Remove(category);

        await _data.SaveChangesAsync();

        await _cache.RemoveByTagAsync(CacheHelpers.CategoryAllCacheTag);
        await _hubNotifier.NotifyCurrentReadingsChangedAsync();

        return Result.Ok(EntityMessage.Create("Category deleted.", category.Id));
    }
}
