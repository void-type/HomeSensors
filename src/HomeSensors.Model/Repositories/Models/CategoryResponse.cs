namespace HomeSensors.Model.Repositories.Models;

public class CategoryResponse
{
    public CategoryResponse(long id, string name, int order)
    {
        Id = id;
        Name = name;
        Order = order;
    }

    public long Id { get; }

    public string Name { get; }

    public int Order { get; }
}
