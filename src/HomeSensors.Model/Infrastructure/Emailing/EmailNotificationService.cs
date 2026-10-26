using HomeSensors.Model.Infrastructure.Emailing.Repositories;
using VoidCore.Model.Emailing;
using VoidCore.Model.Text;

namespace HomeSensors.Model.Infrastructure.Emailing;

/// <summary>
/// This simplifies dependencies for emailing and adds recipients from the database.
/// </summary>
public class EmailNotificationService(IEmailSender emailSender, IEmailFactory emailFactory, NotificationsSettings notificationsSettings,
    TimeProvider timeProvider, EmailRecipientRepository emailRecipientRepository)
{
    public async Task SendAsync(Action<EmailOptionsBuilder> configure, CancellationToken cancellationToken)
    {
        var recipients = await emailRecipientRepository.GetAllEmailsAsync(cancellationToken);

        var newEmail = emailFactory.Create(e =>
        {
            configure.Invoke(e);

            e.AddRecipients(recipients);
            e.AddLine();
            e.AddLine($"Sent from {TextHelpers.Link(notificationsSettings.SignatureName, notificationsSettings.SignatureLink)}");
        });

        await emailSender.SendEmail(newEmail, cancellationToken);
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
            e.AddLine($"Time: {timeProvider.GetUtcNow()}");
        }, default);
    }
}
