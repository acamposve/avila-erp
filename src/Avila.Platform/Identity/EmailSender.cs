namespace Avila.Platform.Identity;

public sealed record EmailMessage(string To, string Subject, string Text);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Local/test sink. It records messages in memory and never sends external mail.
/// </summary>
public sealed class SinkEmailSender : IEmailSender
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => _messages.ToArray();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}

public sealed record SmtpEmailOptions(
    string Host,
    int Port,
    string From,
    string? UserName = null,
    string? Password = null,
    bool EnableSsl = true);

/// <summary>
/// SMTP adapter. Credentials come from configuration/secret management; they
/// are never logged or committed.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(SmtpEmailOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new System.Net.Mail.SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.UserName))
        {
            client.Credentials = new System.Net.NetworkCredential(
                _options.UserName,
                _options.Password);
        }

        using var mail = new System.Net.Mail.MailMessage(
            _options.From,
            message.To,
            message.Subject,
            message.Text);
        await client.SendMailAsync(mail, cancellationToken);
    }
}
