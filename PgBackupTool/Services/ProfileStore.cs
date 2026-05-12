using System.IO;
using System.Text.Json;
using PgBackupTool.Models;

namespace PgBackupTool.Services;

public class ProfileStore
{
    private static string AppDataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PgBackupTool");

    private static string ProfilesPath => Path.Combine(AppDataDir, "profiles.json");

    public async Task<List<ConnectionProfile>> LoadAsync()
    {
        EnsureAppDataDir();
        if (!File.Exists(ProfilesPath))
            return [];

        await using var stream = File.OpenRead(ProfilesPath);
        var doc = await JsonSerializer.DeserializeAsync<ProfilesDocument>(stream);
        return doc?.Profiles ?? [];
    }

    public async Task SaveAsync(IEnumerable<ConnectionProfile> profiles)
    {
        EnsureAppDataDir();
        var doc = new ProfilesDocument { Profiles = profiles.ToList() };
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });

        var tmpPath = ProfilesPath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json);
        File.Move(tmpPath, ProfilesPath, overwrite: true);
    }

    private static void EnsureAppDataDir() =>
        Directory.CreateDirectory(AppDataDir);

    private sealed class ProfilesDocument
    {
        public List<ConnectionProfile> Profiles { get; set; } = [];
    }
}
