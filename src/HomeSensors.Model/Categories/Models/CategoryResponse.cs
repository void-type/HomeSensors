namespace HomeSensors.Model.Categories.Models;

public class CategoryResponse(long id, string name, int order)
{
    public long Id { get; } = id;

    public string Name { get; } = name;

    public int Order { get; } = order;
}
