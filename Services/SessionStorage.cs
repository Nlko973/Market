using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Market.Services
{
    public class SessionState
    {
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public int StoreId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    public static class SessionStorage
    {
        private static readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private static string SessionPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Market", "session.json");

        public static void Save(SessionState state)
        {
            string dir = Path.GetDirectoryName(SessionPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(SessionPath, serializer.Serialize(state));
        }

        public static SessionState Load()
        {
            if (!File.Exists(SessionPath))
            {
                return null;
            }
            try
            {
                var state = serializer.Deserialize<SessionState>(File.ReadAllText(SessionPath));
                if (state == null || state.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    Clear();
                    return null;
                }
                return state;
            }
            catch
            {
                Clear();
                return null;
            }
        }

        public static void Clear()
        {
            if (File.Exists(SessionPath))
            {
                File.Delete(SessionPath);
            }
        }
    }
}
