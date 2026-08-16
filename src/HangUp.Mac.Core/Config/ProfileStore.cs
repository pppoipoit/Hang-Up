using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using HangUp.Mac.Core.Models;

namespace HangUp.Mac.Core.Config
{
    public class ProfileStore
    {
        private List<AppProfile> _profiles;
        public string LastDebugInfo { get; private set; } = "";

        public ProfileStore()
        {
            _profiles = LoadProfiles();
        }

        public List<AppProfile> GetProfiles() => _profiles.ToList();

        public AppProfile? GetProfile(string appName) => 
            _profiles.FirstOrDefault(a => a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

        private List<AppProfile> LoadProfiles()
        {
            string json = "";

            // try file-based first
            string?[] paths = new string?[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apps.json"),
                Path.Combine(AppContext.BaseDirectory, "apps.json"),
                Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "apps.json")),
            };

            foreach (var p in paths)
            {
                if (p != null && File.Exists(p))
                {
                    LastDebugInfo = $"File found at: {p}";
                    json = File.ReadAllText(p);
                    break;
                }
            }

            // fallback to embedded data
            if (string.IsNullOrEmpty(json))
            {
                LastDebugInfo = "Using embedded config (no apps.json file found)";
                json = AppData.GetJson();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<AppsConfig>(json, options);
                if (config?.Apps == null || config.Apps.Count == 0)
                {
                    LastDebugInfo = "Config is empty";
                    return new List<AppProfile>();
                }
                LastDebugInfo = $"Loaded {config.Apps.Count} apps";
                return config.Apps;
            }
            catch (Exception ex)
            {
                LastDebugInfo = $"Parse error: {ex.Message}";
                return new List<AppProfile>();
            }
        }
    }

    public class AppsConfig
    {
        public List<AppProfile> Apps { get; set; } = new();
    }
}
