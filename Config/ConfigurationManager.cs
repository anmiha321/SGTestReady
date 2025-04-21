using System.Text.Json;

namespace SGtest.Config;

public static class ConfigurationManager
{
    public static AppSettings LoadConfiguration(string configPath = "appsettings.json")
    {
        if (!File.Exists(configPath))
        {
            var defaultSettings = new AppSettings
            {
                Database = new DatabaseSettings
                {
                    Host = "localhost",
                    Port = 5432,
                    Database = "postgres",
                    Username = "postgres",
                    Password = "postgres"
                }
            };

            string json = JsonSerializer.Serialize(defaultSettings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
            return defaultSettings;
        }

        string jsonContent = File.ReadAllText(configPath);
        
        return JsonSerializer.Deserialize<AppSettings>(jsonContent);
    }
}