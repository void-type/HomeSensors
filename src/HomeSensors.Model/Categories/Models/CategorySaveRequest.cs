namespace HomeSensors.Model.Categories.Models;

public class CategorySaveRequest
{
    public required long Id { get; init; }

    public required string Name { get; init; } = string.Empty;

    public required int Order { get; init; }
}
