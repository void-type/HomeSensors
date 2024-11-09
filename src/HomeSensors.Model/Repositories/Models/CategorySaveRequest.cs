namespace HomeSensors.Model.Repositories.Models;

public class CategorySaveRequest
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Order { get; init; }
}
