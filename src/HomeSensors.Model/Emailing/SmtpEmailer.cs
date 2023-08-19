using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Emailing;

/// <summary>
/// Send emails using SMTP. Users can be overridden in sub-prod using the Notifications.OverrideEmailRecipients configuration item.
/// </summary>
public class SmtpEmailer : IEmailSender
{
    private readonly NotificationsSettings _notificationsSettings;
    private readonly ApplicationSettings _applicationSettings;
    private readonly ILogger _logger;

    public SmtpEmailer(ILogger<SmtpEmailer> logger, NotificationsSettings notificationsSettings, ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
        _logger = logger;
        _notificationsSettings = notificationsSettings;
    }

    /// <summary>
    /// Send an email.
    /// </summary>
    /// <param name="email">The email to send</param>
    /// <param name="cancellationToken">A cancellation token</param>
    public async Task SendEmail(Email email, CancellationToken cancellationToken)
    {
        if (!_notificationsSettings.IsEnabled)
        {
            _logger.LogWarning("Notifications are disabled. Not sending email \"{Subject}\" to {Recipients}",
                email.Subject,
                email.Recipients);

            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_applicationSettings.Name, _notificationsSettings.FromAddress));
        message.Subject = email.Subject;
        message.Body = new TextPart(_notificationsSettings.EmailTextSubtype)
        {
            Text = email.Message
        };

        ResolveRecipients(email.Recipients)
            .Select(r => new MailboxAddress(r, r))
            .Tee(message.To.AddRange);

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _notificationsSettings.SmtpAddress,
                _notificationsSettings.SmtpPort,
                _notificationsSettings.SmtpSecureSocketOption,
                cancellationToken: cancellationToken);

            if (_notificationsSettings.SmtpUseAuthentication)
            {
                await client.AuthenticateAsync(
                    _notificationsSettings.SmtpUserName,
                    _notificationsSettings.SmtpPassword,
                    cancellationToken);
            }

            _logger.LogInformation("Sending email \"{Subject}\" to {Recipients}",
                message.Subject,
                message.To);

            await client.SendAsync(message);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{nameof(SmtpEmailer)} threw an exception while trying to send email.");
        }
    }

    private IEnumerable<string> ResolveRecipients(IEnumerable<string> originalRecipients)
    {
        var overrideRecipients = _notificationsSettings.OverrideEmailRecipients;

        if (overrideRecipients?.Any() != true)
        {
            return originalRecipients;
        }

        _logger.LogInformation("Overriding original email recipients {OriginalRecipients} with {OverrideEmailRecipients}",
            originalRecipients,
            overrideRecipients);

        return overrideRecipients;
    }
}
