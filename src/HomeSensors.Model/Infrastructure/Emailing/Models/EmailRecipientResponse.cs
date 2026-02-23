namespace HomeSensors.Model.Infrastructure.Emailing.Models;

public class EmailRecipientResponse
{
    public EmailRecipientResponse(long id, string email)
    {
        Id = id;
        Email = email;
    }

    public long Id { get; }

    public string Email { get; }
}
