namespace HomeSensors.Model.Infrastructure.Emailing.Models;

public class EmailRecipientSaveRequest
{
    public long Id { get; init; }

    public string Email { get; init; } = string.Empty;
}
