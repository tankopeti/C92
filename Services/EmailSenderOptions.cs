namespace Cloud9_2.Services
{

public class EmailSenderOptions
{
    // This constant defines the section name in your appsettings/secrets.json
    public const string SectionName = "EmailSender";

    // Property to hold the SMTP server address
    public string SmtpServer { get; set; } // Will hold "smtp.gmail.com" from config

    // Property to hold the SMTP port number
    public int SmtpPort { get; set; } // Will hold 587 from config

    // Property to hold the display name for the sender
    public string SenderName { get; set; } // Will hold "Cloud9.2" from config

    // Property to hold the sender's email address (the "From" address)
    public string SenderEmail { get; set; } // Will hold "tankopeti@gmail.com" from config

    // Property to hold the username for SMTP authentication
    public string SmtpUser { get; set; } // Will hold "tankopeti" from config (or often the full email for Gmail)

    // Property to hold the password for SMTP authentication
    // The ACTUAL password value should be in secrets.json, NOT here.
    public string SmtpPass { get; set; } // Will hold "HUmmer513" (or preferably an App Password) from secrets.json

    // Property to indicate whether to explicitly connect using SSL/TLS on initial connection
    // Usually true for port 465, false for ports 587/25 (which typically use STARTTLS after connecting)
    public bool UseSsl { get; set; } // Will hold true/false from config (likely false for port 587)
}}