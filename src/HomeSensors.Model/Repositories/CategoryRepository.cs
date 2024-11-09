using HomeSensors.Model.Data;
using HomeSensors.Model.Data.Models;
using HomeSensors.Model.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Repositories;

public class CategoryRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public CategoryRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public async Task<List<CategoryResponse>> GetAll()
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

    public async Task<IResult<EntityMessage<long>>> Save(CategorySaveRequest request)
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

        return Result.Ok(EntityMessage.Create("Category saved.", category.Id));
    }

    public async Task<IResult<EntityMessage<long>>> Delete(int id)
    {
        var category = await _data.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Category not found."));
        }

        _data.Categories.Remove(category);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Category deleted.", category.Id));
    }
}
