#nullable disable
using Mentornote.Backend.Models;
using System.Text.Json;
using System.IO;

namespace Mentornote.Backend.Services
{
    public class SettingsService
    {
        private const string FilePath = "settings.json";

        public AppSettings Current { get; private set; }

       

        public void Update(Action<AppSettings> update)
        {
            update(Current);
            Save();
        }

        private void Load()
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<AppSettings>(json)!;
            }
            else
            {
                Current = new AppSettings();
                Save();
            }
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(FilePath, json);
        }
    }
}
