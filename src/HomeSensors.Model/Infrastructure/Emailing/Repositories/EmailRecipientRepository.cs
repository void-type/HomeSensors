using HomeSensors.Model.Data;
using HomeSensors.Model.Infrastructure.Emailing.Entities;
using HomeSensors.Model.Infrastructure.Emailing.Models;
using Microsoft.EntityFrameworkCore;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Messages;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Infrastructure.Emailing.Repositories;

public class EmailRecipientRepository : RepositoryBase
{
    private readonly HomeSensorsContext _data;

    public EmailRecipientRepository(HomeSensorsContext data)
    {
        _data = data;
    }

    public async Task<List<EmailRecipientResponse>> GetAllAsync()
    {
        return await _data.EmailRecipients
            .TagWith(GetTag())
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .Select(x => new EmailRecipientResponse(x.Id, x.Email))
            .ToListAsync();
    }

    public async Task<List<string>> GetAllEmailsAsync()
    {
        return await _data.EmailRecipients
            .TagWith(GetTag())
            .AsNoTracking()
            .Select(x => x.Email)
            .ToListAsync();
    }

    public async Task<IResult<EntityMessage<long>>> SaveAsync(EmailRecipientSaveRequest request)
    {
        if (request.Email.IsNullOrWhiteSpace())
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Email is required.", "email"));
        }

        var recipient = await _data.EmailRecipients
            .FirstOrDefaultAsync(x => x.Id == request.Id);

        if (recipient is null)
        {
            recipient = new EmailRecipient();
            _data.EmailRecipients.Add(recipient);
        }

        recipient.Email = request.Email;

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Email recipient saved.", recipient.Id));
    }

    public async Task<IResult<EntityMessage<long>>> DeleteAsync(long id)
    {
        var recipient = await _data.EmailRecipients
            .FirstOrDefaultAsync(x => x.Id == id);

        if (recipient is null)
        {
            return Result.Fail<EntityMessage<long>>(new Failure("Email recipient not found."));
        }

        _data.EmailRecipients.Remove(recipient);

        await _data.SaveChangesAsync();

        return Result.Ok(EntityMessage.Create("Email recipient deleted.", recipient.Id));
    }
}
