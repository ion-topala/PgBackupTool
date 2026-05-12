using System.Globalization;
using System.IO;
using PgBackupTool.Models;

namespace PgBackupTool.Services;

public class BackupCatalog
{
    private static string BackupsRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PgBackupTool", "backups");

    public IEnumerable<BackupEntry> ListBackups(ConnectionProfile? filterByProfile = null)
    {
        var root = filterByProfile != null
            ? Path.Combine(BackupsRoot, filterByProfile.Name)
            : BackupsRoot;

        if (!Directory.Exists(root))
            return [];

        var pattern = filterByProfile != null ? "*.dump" : "**/*.dump";
        var files = filterByProfile != null
            ? Directory.EnumerateFiles(root, "*.dump")
            : Directory.EnumerateFiles(root, "*.dump", SearchOption.AllDirectories);

        return files
            .Select(BuildEntry)
            .OrderByDescending(e => e.CreatedAt);
    }

    public void DeleteBackup(string filePath) => File.Delete(filePath);

    private static BackupEntry BuildEntry(string filePath)
    {
        var info = new FileInfo(filePath);
        var entry = new BackupEntry
        {
            FilePath = filePath,
            FileName = info.Name,
            SizeBytes = info.Length,
        };

        // Parse "<profileName>_<yyyyMMdd_HHmmss>.dump"
        var nameWithoutExt = Path.GetFileNameWithoutExtension(info.Name);
        var underscoreIdx = nameWithoutExt.LastIndexOf('_', nameWithoutExt.Length - 1 - 7);
        if (underscoreIdx > 0)
        {
            var dateTimePart = nameWithoutExt[(underscoreIdx + 1)..];
            var profilePart = nameWithoutExt[..underscoreIdx];

            // dateTimePart is "yyyyMMdd_HHmmss" — strip inner underscore for parsing
            var normalized = dateTimePart.Replace("_", "");
            if (DateTime.TryParseExact(normalized, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
            {
                entry.CreatedAt = dt;
                entry.ProfileName = profilePart;
                return entry;
            }
        }

        // Fallback
        entry.CreatedAt = info.CreationTime;
        entry.ProfileName = info.Directory?.Name ?? "";
        return entry;
    }
}
