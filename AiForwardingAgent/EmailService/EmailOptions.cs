namespace AiForwardingAgent.EmailService;

public class EmailOptions
{
    public const string SectionName = "Email";

    // SMTP (Versand)
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;

    // IMAP (Lesen)
    public string ImapHost { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;
    public bool ImapEnableSsl { get; set; } = true;
    public string ImapInboxFolder { get; set; } = "INBOX";

    // Gemeinsame Zugangsdaten
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Nur für Diagnosezwecke: kann bei Netzwerken mit blockiertem CRL/OCSP helfen
    public bool DisableCertificateRevocationCheck { get; set; }

    // Absenderdaten
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}