using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using AiForwardingAgent.Data;


namespace AiForwardingAgent.EmailService;

public partial class EMailMailboxService(EmailOptions smtpOptions) :
    IEmailMailboxService,
    IEmailSender<ApplicationUser>,
    Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    private readonly EmailOptions _smtpOptions = smtpOptions ?? throw new ArgumentNullException(nameof(smtpOptions));

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(
            email,
            user.UserName ?? email,
            "Confirm your email",
            $"Please confirm your account by visiting this link: {confirmationLink}",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.",
            CancellationToken.None);

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(
            email,
            user.UserName ?? email,
            "Reset your password",
            $"Please reset your password by visiting this link: {resetLink}",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.",
            CancellationToken.None);

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(
            email,
            user.UserName ?? email,
            "Reset your password",
            $"Please reset your password using the following code: {resetCode}",
            $"Please reset your password using the following code: <strong>{WebUtility.HtmlEncode(resetCode)}</strong>",
            CancellationToken.None);

    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        SendEmailAsync(
            email,
            email,
            subject,
            ConvertHtmlToPlainText(htmlMessage),
            htmlMessage,
            CancellationToken.None);

    public async Task<IReadOnlyList<MailboxEmailMessage>> ReadInboxAsync(int maxCount = 20, bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        ValidateImapConfiguration();

        using var imapClient = new ImapClient();
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(_smtpOptions.UserName, _smtpOptions.Password, cancellationToken);
        }

        var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        var query = unreadOnly ? SearchQuery.NotSeen : SearchQuery.All;
        var uids = await inbox.SearchAsync(query, cancellationToken);

        if (uids.Count == 0)
        {
            await imapClient.DisconnectAsync(true, cancellationToken);
            return [];
        }

        var selectedUids = uids
            .OrderByDescending(x => x.Id)
            .Take(maxCount)
            .ToList();

        var summaries = await inbox.FetchAsync(
            selectedUids,
            MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.InternalDate,
            cancellationToken);

        var result = summaries
            .OrderByDescending(x => x.InternalDate ?? DateTimeOffset.MinValue)
            .Select(summary => new MailboxEmailMessage
            {
                Id = summary.UniqueId.Id.ToString(),
                From = summary.Envelope?.From?.Mailboxes.FirstOrDefault()?.Address
                       ?? summary.Envelope?.From?.ToString()
                       ?? string.Empty,
                Subject = summary.Envelope?.Subject ?? string.Empty,
                ReceivedAt = summary.InternalDate,
                IsRead = summary.Flags?.HasFlag(MessageFlags.Seen) == true
            })
            .ToList();

        await imapClient.DisconnectAsync(true, cancellationToken);
        return result;
    }

    public async Task<MailboxEmailContent> ReadMessageContentAsync(
        string messageId, CancellationToken cancellationToken = default)
    {
        ValidateImapConfiguration();
        var uid = ParseUniqueId(messageId);

        using var imapClient = new ImapClient();
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(_smtpOptions.UserName, _smtpOptions.Password, cancellationToken);
        }

        var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        MimeMessage message;
        try
        {
            message = await inbox.GetMessageAsync(uid, cancellationToken);
        }
        catch (MessageNotFoundException)
        {
            throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
        }

        var textBody = message.TextBody;
        var isConvertedFromHtml = false;

        if (string.IsNullOrWhiteSpace(textBody) && !string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            textBody = ConvertHtmlToPlainText(message.HtmlBody);
            isConvertedFromHtml = true;
        }

        await imapClient.DisconnectAsync(true, cancellationToken);

        return new MailboxEmailContent
        {
            BodyText = textBody ?? string.Empty,
            IsConvertedFromHtml = isConvertedFromHtml
        };
    }

    public async Task MarkAsReadAsync(string messageId, CancellationToken cancellationToken = default)
    
    {
        ValidateImapConfiguration();

        var uid = ParseUniqueId(messageId);

        using var imapClient = new ImapClient();
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(_smtpOptions.UserName, _smtpOptions.Password, cancellationToken);
        }

        var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var summaries = await inbox.FetchAsync(
            [uid],
            MessageSummaryItems.UniqueId,
            cancellationToken);

        if (summaries.Count == 0)
        {
            throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
        }

        await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
        await imapClient.DisconnectAsync(true, cancellationToken);
    }
    
    public async Task MarkAsFlaggedAsync(string messageId, CancellationToken cancellationToken = default)
    
    {
        ValidateImapConfiguration();

        var uid = ParseUniqueId(messageId);

        using var imapClient = new ImapClient();
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(_smtpOptions.UserName, _smtpOptions.Password, cancellationToken);
        }

        var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var summaries = await inbox.FetchAsync(
            [uid],
            MessageSummaryItems.UniqueId,
            cancellationToken);

        if (summaries.Count == 0)
        {
            throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
        }

        await inbox.AddFlagsAsync(uid, MessageFlags.Flagged, true, cancellationToken);
        await imapClient.DisconnectAsync(true, cancellationToken);
    }

    public async Task DeleteMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ValidateImapConfiguration();

        var uid = ParseUniqueId(messageId);

        using var imapClient = new ImapClient();
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(_smtpOptions.UserName, _smtpOptions.Password, cancellationToken);
        }

        IMailFolder inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        var summaries = await inbox.FetchAsync(
            [uid],
            MessageSummaryItems.UniqueId,
            cancellationToken);

        if (summaries.Count == 0)
        {
            throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
        }

        await inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true, cancellationToken);
        await inbox.ExpungeAsync(cancellationToken);
        await imapClient.DisconnectAsync(true, cancellationToken);
    }

    public async Task ReplyToMessageAsync(string messageId, string textBody, string? htmlBody = null, string? subjectPrefix = null,
    CancellationToken cancellationToken = default)
{
    ValidateImapConfiguration();
    ValidateSmtpConfiguration();

    var uid = ParseUniqueId(messageId);

    MimeMessage originalMessage;

    using (var imapClient = new ImapClient())
    {
        imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
        await imapClient.ConnectAsync(
            _smtpOptions.ImapHost,
            _smtpOptions.ImapPort,
            GetImapSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await imapClient.AuthenticateAsync(
                _smtpOptions.UserName,
                _smtpOptions.Password,
                cancellationToken);
        }

        var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
            ? imapClient.Inbox
            : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        try
        {
            originalMessage = await inbox.GetMessageAsync(uid, cancellationToken);
        }
        catch (MessageNotFoundException)
        {
            throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
        }

        await imapClient.DisconnectAsync(true, cancellationToken);
    }

    var replyMessage = new MimeMessage();

    var fromAddress = string.IsNullOrWhiteSpace(_smtpOptions.FromAddress)
        ? _smtpOptions.UserName
        : _smtpOptions.FromAddress;

    var fromName = string.IsNullOrWhiteSpace(_smtpOptions.FromName)
        ? _smtpOptions.UserName
        : _smtpOptions.FromName;

    replyMessage.From.Add(new MailboxAddress(fromName, fromAddress));

    if (originalMessage.ReplyTo.Count > 0)
    {
        replyMessage.To.AddRange(originalMessage.ReplyTo.Mailboxes);
    }
    else if (originalMessage.From.Count > 0)
    {
        replyMessage.To.AddRange(originalMessage.From.Mailboxes);
    }
    else
    {
        throw new InvalidOperationException("Die Originalnachricht enthält keinen gültigen Antwortempfänger.");
    }

    replyMessage.Subject = string.IsNullOrWhiteSpace(originalMessage.Subject)
        ? "Re:"
        : originalMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            ? originalMessage.Subject
            : $"Re: {originalMessage.Subject}";

    if (!string.IsNullOrWhiteSpace(subjectPrefix))
    {
        replyMessage.Subject = $"{subjectPrefix} {replyMessage.Subject}";
    }
    
    if (!string.IsNullOrWhiteSpace(originalMessage.MessageId))
    {
        replyMessage.InReplyTo = originalMessage.MessageId;

        foreach (var reference in originalMessage.References)
        {
            replyMessage.References.Add(reference);
        }

        replyMessage.References.Add(originalMessage.MessageId);
    }

    var bodyBuilder = new BodyBuilder
    {
        TextBody = BuildReplyTextBody(textBody, originalMessage)
    };

    if (!string.IsNullOrWhiteSpace(htmlBody))
    {
        bodyBuilder.HtmlBody = BuildReplyHtmlBody(htmlBody, originalMessage);
    }

    replyMessage.Body = bodyBuilder.ToMessageBody();

    using var smtpClient = new SmtpClient();
    smtpClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;

    await smtpClient.ConnectAsync(
        _smtpOptions.Host,
        _smtpOptions.Port,
        GetSmtpSocketOptions(),
        cancellationToken);

    if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
    {
        await smtpClient.AuthenticateAsync(
            _smtpOptions.UserName,
            _smtpOptions.Password,
            cancellationToken);
    }

    await smtpClient.SendAsync(replyMessage, cancellationToken);
    await smtpClient.DisconnectAsync(true, cancellationToken);
}

    public async Task ForwardToMessageAsync(string toAddress, string toName, string messageId, string textBody, string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        ValidateImapConfiguration();
        ValidateSmtpConfiguration();

        var uid = ParseUniqueId(messageId);

        MimeMessage originalMessage;

        using (var imapClient = new ImapClient())
        {
            imapClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;
            await imapClient.ConnectAsync(
                _smtpOptions.ImapHost,
                _smtpOptions.ImapPort,
                GetImapSocketOptions(),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
            {
                await imapClient.AuthenticateAsync(
                    _smtpOptions.UserName,
                    _smtpOptions.Password,
                    cancellationToken);
            }

            var inbox = _smtpOptions.ImapInboxFolder.Equals("INBOX", StringComparison.OrdinalIgnoreCase)
                ? imapClient.Inbox
                : await imapClient.GetFolderAsync(_smtpOptions.ImapInboxFolder, cancellationToken);

            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            try
            {
                originalMessage = await inbox.GetMessageAsync(uid, cancellationToken);
            }
            catch (MessageNotFoundException)
            {
                throw new InvalidOperationException("Die ausgewählte E-Mail wurde nicht gefunden.");
            }

            await imapClient.DisconnectAsync(true, cancellationToken);
        }

        var forwardMessage = new MimeMessage();

        var fromAddress = string.IsNullOrWhiteSpace(_smtpOptions.FromAddress)
            ? _smtpOptions.UserName
            : _smtpOptions.FromAddress;

        var fromName = string.IsNullOrWhiteSpace(_smtpOptions.FromName)
            ? _smtpOptions.UserName
            : _smtpOptions.FromName;

        forwardMessage.From.Add(new MailboxAddress(fromName, fromAddress));

        forwardMessage.To.Add(new MailboxAddress(toName, toAddress));

        forwardMessage.Subject = string.IsNullOrWhiteSpace(originalMessage.Subject)
            ? "Re:"
            : originalMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
                ? originalMessage.Subject
                : $"Re: {originalMessage.Subject}";

        if (!string.IsNullOrWhiteSpace(originalMessage.MessageId))
        {
            forwardMessage.InReplyTo = originalMessage.MessageId;

            foreach (var reference in originalMessage.References)
            {
                forwardMessage.References.Add(reference);
            }

            forwardMessage.References.Add(originalMessage.MessageId);
        }

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildReplyTextBody(textBody, originalMessage)
        };

        if (!string.IsNullOrWhiteSpace(htmlBody))
        {
            bodyBuilder.HtmlBody = BuildReplyHtmlBody(htmlBody, originalMessage);
        }

        forwardMessage.Body = bodyBuilder.ToMessageBody();

        using var smtpClient = new SmtpClient();
        smtpClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;

        await smtpClient.ConnectAsync(
            _smtpOptions.Host,
            _smtpOptions.Port,
            GetSmtpSocketOptions(),
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await smtpClient.AuthenticateAsync(
                _smtpOptions.UserName,
                _smtpOptions.Password,
                cancellationToken);
        }

        await smtpClient.SendAsync(forwardMessage, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

    }
    public async Task SendEmailAsync(string toAddress, string toName, string subject, string textBody, string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSmtpConfiguration();
        
        var replyMessage = new MimeMessage();
        var fromAddress = string.IsNullOrWhiteSpace(_smtpOptions.FromAddress)
            ? _smtpOptions.UserName
            : _smtpOptions.FromAddress;
        
        var fromName = string.IsNullOrWhiteSpace(_smtpOptions.FromName)
            ? _smtpOptions.UserName
            : _smtpOptions.FromName;
        
        replyMessage.From.Add(new MailboxAddress(fromName, fromAddress));
        replyMessage.To.Add(new MailboxAddress(toName,toAddress));
        replyMessage.Subject = subject;
        replyMessage.Body = new BodyBuilder
        {
            TextBody = textBody,
            HtmlBody = htmlBody
        }.ToMessageBody();
        

        using var smtpClient = new SmtpClient();
        smtpClient.CheckCertificateRevocation = !_smtpOptions.DisableCertificateRevocationCheck;

        await smtpClient.ConnectAsync(
            _smtpOptions.Host,
            _smtpOptions.Port,
            GetSmtpSocketOptions(),
            cancellationToken);


        if (!string.IsNullOrWhiteSpace(_smtpOptions.UserName))
        {
            await smtpClient.AuthenticateAsync(
                _smtpOptions.UserName,
                _smtpOptions.Password,
                cancellationToken);
        }

        await smtpClient.SendAsync(replyMessage, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);
    }
    
    

    private void ValidateImapConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.ImapHost))
        {
            throw new InvalidOperationException(
                "IMAP configuration is incomplete. Configure Email:ImapHost and related settings.");
        }

        if (string.IsNullOrWhiteSpace(_smtpOptions.UserName) != string.IsNullOrWhiteSpace(_smtpOptions.Password))
        {
            throw new InvalidOperationException(
                "IMAP configuration is invalid. Set both UserName and Password, or leave both empty.");
        }
    }
    private void ValidateSmtpConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_smtpOptions.Host) ||
            string.IsNullOrWhiteSpace(_smtpOptions.FromAddress))
        {
            throw new InvalidOperationException(
                "SMTP configuration is incomplete. Configure the Email section in appsettings or user secrets.");
        }

        if (string.IsNullOrWhiteSpace(_smtpOptions.UserName) != string.IsNullOrWhiteSpace(_smtpOptions.Password))
        {
            throw new InvalidOperationException(
                "SMTP configuration is invalid. Set both UserName and Password, or leave both empty.");
        }
    }
    
    private SecureSocketOptions GetSmtpSocketOptions()
    {
        return _smtpOptions.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => _smtpOptions.EnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
        };
    }
    
    private SecureSocketOptions GetImapSocketOptions()
    {
        return _smtpOptions.ImapPort switch
        {
            993 => SecureSocketOptions.SslOnConnect,
            143 => SecureSocketOptions.StartTls,
            _ => _smtpOptions.ImapEnableSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
        };
    }
    
    private static UniqueId ParseUniqueId(string messageId)
    {
        if (!uint.TryParse(messageId, out var uidValue))
        {
            throw new InvalidOperationException("Die Nachrichten-ID ist ungültig.");
        }

        return new UniqueId(uidValue);
    }
    
    private static string BuildReplyTextBody(string replyText, MimeMessage originalMessage)
    {
        var originalText = originalMessage.TextBody;

        if (string.IsNullOrWhiteSpace(originalText) && !string.IsNullOrWhiteSpace(originalMessage.HtmlBody))
        {
            originalText = ConvertHtmlToPlainText(originalMessage.HtmlBody);
        }

        if (string.IsNullOrWhiteSpace(originalText))
        {
            return replyText;
        }

        var quotedLines = originalText
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => $"> {line}");

        return $"{replyText}{Environment.NewLine}{Environment.NewLine}" +
               $"----- Original Message -----{Environment.NewLine}" +
               string.Join(Environment.NewLine, quotedLines);
    }
    
    private static string BuildReplyHtmlBody(string htmlBody, MimeMessage originalMessage)
    {
        var originalHtml = originalMessage.HtmlBody;

        if (string.IsNullOrWhiteSpace(originalHtml) && !string.IsNullOrWhiteSpace(originalMessage.TextBody))
        {
            var encodedText = WebUtility.HtmlEncode(originalMessage.TextBody)
                .Replace("\r\n", "<br />")
                .Replace("\n", "<br />");

            originalHtml = $"<pre>{encodedText}</pre>";
        }

        if (string.IsNullOrWhiteSpace(originalHtml))
        {
            return htmlBody;
        }

        return $"""
                <div>{htmlBody}</div>
                <br />
                <hr />
                <div>
                    <strong>Original Message</strong><br />
                    {originalHtml}
                </div>
                """;
    }
    
    private static string ConvertHtmlToPlainText(string htmlBody)
    {
        var withoutTags = MyRegex().Replace(htmlBody, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags);
        return MyRegex1().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex MyRegex();
    [GeneratedRegex("\\s+")]
    private static partial Regex MyRegex1();
}