using System.IO;
using PgBackupTool.Models;

namespace PgBackupTool.Services;

public record BackupResult(bool Success, string FilePath, string ErrorMessage);

public class BackupService
{
    private static string BackupsRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PgBackupTool", "backups");

    public async Task<BackupResult> CreateBackupAsync(
        ConnectionProfile profile,
        IProgress<string> log,
        CancellationToken ct)
    {
        var dir = Path.Combine(BackupsRoot, profile.Name);
        Directory.CreateDirectory(dir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outputPath = Path.Combine(dir, $"{profile.Name}_{timestamp}.dump");

        log.Report($"[backup] Starting backup of '{profile.Database}' → {outputPath}");

        var lastLines = new Queue<string>(10);
        var wrappedLog = new Progress<string>(line =>
        {
            log.Report(line);
            lastLines.Enqueue(line);
            if (lastLines.Count > 10)
                lastLines.Dequeue();
        });

        var args = new[]
        {
            $"--host={profile.Host}",
            $"--port={profile.Port}",
            $"--username={profile.Username}",
            $"--dbname={profile.Database}",
            "--format=custom",
            $"--file={outputPath}",
            "--verbose",
            "--no-password",
        };

        var env = new Dictionary<string, string> { ["PGPASSWORD"] = profile.Password };

        int exitCode;
        try
        {
            exitCode = await ProcessRunner.RunAsync("pg_dump", args, env, wrappedLog, ct);
        }
        catch (OperationCanceledException)
        {
            log.Report("[backup] Cancelled — deleting partial file.");
            TryDelete(outputPath);
            return new BackupResult(false, outputPath, "Cancelled by user.");
        }
        catch (Exception ex)
        {
            TryDelete(outputPath);
            return new BackupResult(false, "", $"Failed to launch pg_dump: {ex.Message}");
        }

        if (exitCode != 0)
        {
            log.Report($"[backup] pg_dump exited with code {exitCode} — deleting partial file.");
            TryDelete(outputPath);
            var errorSummary = string.Join(Environment.NewLine, lastLines);
            return new BackupResult(false, "", $"pg_dump failed (exit {exitCode}):\n{errorSummary}");
        }

        log.Report($"[backup] Done. File: {outputPath}");
        return new BackupResult(true, outputPath, "");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort */ }
    }
}
