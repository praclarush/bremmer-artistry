namespace PotteryJournal.Infrastructure.Options
{
    /// <summary>
    /// SMTP connection details for outbound email (class booking notifications, contact form
    /// submissions), bound from the "Smtp" section. These are deployment secrets, supplied via
    /// environment variables/user-secrets -- never committed to appsettings.json.
    /// </summary>
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string User { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "Bremmer Artistry";

        public bool UseStartTls { get; set; } = true;
    }
}
