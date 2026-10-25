namespace HomeSensors.Model.Infrastructure.Emailing.Models;

public class EmailRecipientResponse(long id, string email)
{
    public long Id { get; } = id;

    public string Email { get; } = email;
}
