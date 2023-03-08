using MailKit.Security;
using VoidCore.Model.Guards;

namespace HomeSensors.Service.Emailing;

public class NotificationsSettings
{
    public bool IsEnabled { get; set; } = true;
    public IEnumerable<string>? EmailRecipients { get; init; }
    public IEnumerable<string>? OverrideEmailRecipients { get; init; }
    public string EmailTextSubtype { get; set; } = "html";
    public string FromAddress { get; init; } = string.Empty;
    public string SmtpAddress { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
    public SecureSocketOptions SmtpSecureSocketOption { get; set; } = SecureSocketOptions.Auto;
    public bool SmtpUseAuthentication { get; init; }
    public string SmtpUserName { get; init; } = string.Empty;
    public string SmtpPassword { get; init; } = string.Empty;
    public string SignatureName { get; set; } = string.Empty;
    public string SignatureLink { get; set; } = string.Empty;

    /// <summary>
    /// Check the state of this configuration and throw an exception if it is invalid.
    /// </summary>
    public NotificationsSettings Validate()
    {
        const string notFoundMessage = "Property not found in application configuration.";

        EmailTextSubtype.EnsureNotNullOrEmpty(message: notFoundMessage);
        FromAddress.EnsureNotNullOrEmpty(message: notFoundMessage);
        SmtpAddress.EnsureNotNullOrEmpty(message: notFoundMessage);

        if (SmtpUseAuthentication)
        {
            SmtpUserName.EnsureNotNullOrEmpty(message: notFoundMessage);
            SmtpPassword.EnsureNotNullOrEmpty(message: notFoundMessage);
        }

        return this;
    }
}
