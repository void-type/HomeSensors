using VoidCore.Model.Emailing;
using VoidCore.Model.Text;

namespace HomeSensors.Service.Emailing;

/// <summary>
/// This simplifies dependencies for emailing and adds recipients from configuration.
/// </summary>
public class EmailNotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly IEmailFactory _emailFactory;
    private readonly NotificationsSettings _notificationsSettings;

    public EmailNotificationService(IEmailSender emailSender, IEmailFactory emailFactory, NotificationsSettings notificationsSettings)
    {
        _emailSender = emailSender;
        _emailFactory = emailFactory;
        _notificationsSettings = notificationsSettings;
    }

    public Task Send(Action<EmailOptionsBuilder> configure, CancellationToken cancellationToken)
    {
        var newEmail = _emailFactory.Create(e =>
        {
            configure.Invoke(e);

            e.AddRecipients(_notificationsSettings.EmailRecipients ?? Enumerable.Empty<string>());
            e.AddLine();
            e.AddLine($"Sent from {TextHelpers.Link(_notificationsSettings.SignatureName, _notificationsSettings.SignatureLink)}");
        });

        return _emailSender.SendEmail(newEmail, cancellationToken);
    }
}
