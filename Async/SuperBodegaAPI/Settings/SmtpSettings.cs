namespace SuperBodegaAPI.Settings
{
    /// <summary>
    /// Corresponde a la sección "Smtp" de appsettings.json
    /// </summary>
    public class SmtpSettings
    {
        public string Host       { get; set; } = string.Empty;
        public int    Port       { get; set; }
        public string User       { get; set; } = string.Empty;
        public string Password   { get; set; } = string.Empty;

        public string FromEmail  { get; set; } = string.Empty; // ✅ NUEVO
        public string FromName   { get; set; } = string.Empty; // ✅ NUEVO
    }
}
