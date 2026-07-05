using System.Configuration;

namespace Market.Services
{
    public static class BackendSettings
    {
        public static string BaseUrl
        {
            get
            {
                var configuredUrl = ConfigurationManager.AppSettings["BackendBaseUrl"];
                return string.IsNullOrWhiteSpace(configuredUrl)
                    ? "http://127.0.0.1:8000"
                    : configuredUrl.TrimEnd('/');
            }
        }
    }
}
