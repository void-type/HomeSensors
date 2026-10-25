using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using VoidCore.Model.Configuration;
using VoidCore.Model.Emailing;
using VoidCore.Model.Functional;

namespace HomeSensors.Model.Infrastructure.Emailing;

/// <summary>
/// Send emails using SMTP. Users can be overridden in sub-prod using the Notifications.OverrideEmailRecipients configuration item.
/// </summary>
public class SmtpEmailer(ILogger<SmtpEmailer> logger, NotificationsSettings notificationsSettings, ApplicationSettings applicationSettings) : IEmailSender
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Send an email.
    /// </summary>
    /// <param name="email">The email to send</param>
    /// <param name="cancellationToken">A cancellation token</param>
    public async Task SendEmail(Email email, CancellationToken cancellationToken)
    {
        if (!notificationsSettings.IsEnabled)
        {
            _logger.LogWarning("Notifications are disabled. Not sending email \"{Subject}\" to {Recipients}",
                email.Subject,
                email.Recipients);

            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(applicationSettings.Name, notificationsSettings.FromAddress));
        message.Subject = email.Subject;
        message.Body = new TextPart(notificationsSettings.EmailTextSubtype)
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
                notificationsSettings.SmtpAddress,
                notificationsSettings.SmtpPort,
                notificationsSettings.SmtpSecureSocketOption,
                cancellationToken: cancellationToken);

            if (notificationsSettings.SmtpUseAuthentication)
            {
                await client.AuthenticateAsync(
                    notificationsSettings.SmtpUserName,
                    notificationsSettings.SmtpPassword,
                    cancellationToken);
            }

            _logger.LogInformation("Sending email \"{Subject}\" to {Recipients}",
                message.Subject,
                message.To);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ClassName} threw an exception while trying to send email.", nameof(SmtpEmailer));
        }
    }

    private IEnumerable<string> ResolveRecipients(IEnumerable<string> originalRecipients)
    {
        var overrideRecipients = notificationsSettings.OverrideEmailRecipients;

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
