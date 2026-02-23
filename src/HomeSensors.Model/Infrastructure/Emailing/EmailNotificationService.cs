using HomeSensors.Model.Infrastructure.Emailing.Repositories;
using VoidCore.Model.Emailing;
using VoidCore.Model.Text;
using VoidCore.Model.Time;

namespace HomeSensors.Model.Infrastructure.Emailing;

/// <summary>
/// This simplifies dependencies for emailing and adds recipients from the database.
/// </summary>
public class EmailNotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailFactory _emailFactory;
    private readonly NotificationsSettings _notificationsSettings;
    private readonly IDateTimeService _dateTimeService;
    private readonly EmailRecipientRepository _emailRecipientRepository;

    public EmailNotificationService(IEmailSender emailSender, IEmailFactory emailFactory, NotificationsSettings notificationsSettings,
        IDateTimeService dateTimeService, EmailRecipientRepository emailRecipientRepository)
    {
        _emailSender = emailSender;
        _emailFactory = emailFactory;
        _notificationsSettings = notificationsSettings;
        _dateTimeService = dateTimeService;
        _emailRecipientRepository = emailRecipientRepository;
    }

    public async Task SendAsync(Action<EmailOptionsBuilder> configure, CancellationToken cancellationToken)
    {
        var recipients = await _emailRecipientRepository.GetAllEmailsAsync();

        var newEmail = _emailFactory.Create(e =>
        {
            configure.Invoke(e);

            e.AddRecipients(recipients);
            e.AddLine();
            e.AddLine($"Sent from {TextHelpers.Link(_notificationsSettings.SignatureName, _notificationsSettings.SignatureLink)}");
        });

        await _emailSender.SendEmail(newEmail, cancellationToken);
    }

    public async Task NotifyErrorAsync(string bodyMessage, string? subject = null, Exception? exception = null)
    {
        await SendAsync(e =>
        {
            e.SetSubject($"Error occurred: {subject ?? bodyMessage}");

            e.AddLine($"Error occurred: {bodyMessage}");

            if (exception is not null)
            {
                e.AddLine();
                e.AddLine("Exception:");
                e.AddLine(exception.ToString());
            }

            e.AddLine();
            e.AddLine($"Time: {_dateTimeService.MomentWithOffset}");
        }, default);
    }
}
