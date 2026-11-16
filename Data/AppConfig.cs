using System.Configuration;

namespace CarSparePartsSystem.Data
{
    /// <summary>
    /// NFR-12: Single place that reads the connection string from App.config.
    /// All controllers and forms use AppConfig.ConnectionString instead of
    /// hard-coding the value.
    /// </summary>
    public static class AppConfig
    {
        public static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["CarSparePartsDB"].ConnectionString;
    }
}
