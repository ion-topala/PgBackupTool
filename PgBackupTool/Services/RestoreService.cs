using System.IO;
using PgBackupTool.Models;

namespace PgBackupTool.Services;

public record RestoreResult(bool Success, string ErrorMessage);

public class RestoreService
{
    public async Task<RestoreResult> RestoreAsync(
        ConnectionProfile profile,
        string backupFilePath,
        IProgress<string> log,
        CancellationToken ct)
    {
        if (!File.Exists(backupFilePath))
            return new RestoreResult(false, $"Backup file not found: {backupFilePath}");

        log.Report($"[restore] *** WARNING: Dropping and recreating '{profile.Database}' on {profile.Host}:{profile.Port} ***");
        log.Report($"[restore] Using backup: {backupFilePath}");

        var env = new Dictionary<string, string> { ["PGPASSWORD"] = profile.Password };
        var safeDb = EscapeDbName(profile.Database);

        // Step 1 — terminate existing connections
        log.Report("[restore] Terminating existing connections...");
        var terminateSql = $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{safeDb.Replace("'", "''")}' AND pid <> pg_backend_pid();";
        var result = await RunPsqlAsync(profile, "postgres", terminateSql, env, log, ct);
        if (!result) return new RestoreResult(false, "Failed to terminate existing connections.");

        // Step 2 — drop database
        log.Report($"[restore] Dropping database \"{safeDb}\"...");
        result = await RunPsqlAsync(profile, "postgres", $"DROP DATABASE IF EXISTS \"{safeDb}\";", env, log, ct);
        if (!result) return new RestoreResult(false, $"Failed to drop database \"{safeDb}\". Database may be in an unknown state.");

        // Step 3 — create empty database
        log.Report($"[restore] Creating database \"{safeDb}\"...");
        result = await RunPsqlAsync(profile, "postgres", $"CREATE DATABASE \"{safeDb}\";", env, log, ct);
        if (!result) return new RestoreResult(false, $"Failed to create database \"{safeDb}\". Database may be in an unknown state.");

        // Step 4 — pg_restore
        log.Report("[restore] Restoring data...");
        var restoreArgs = new[]
        {
            $"--host={profile.Host}",
            $"--port={profile.Port}",
            $"--username={profile.Username}",
            $"--dbname={profile.Database}",
            "--no-password",
            "--verbose",
            "--exit-on-error",
            backupFilePath,
        };

        int exitCode;
        try
        {
            exitCode = await ProcessRunner.RunAsync("pg_restore", restoreArgs, env, log, ct);
        }
        catch (OperationCanceledException)
        {
            log.Report("[restore] CANCELLED — database is in an unknown/partial state. Retry with a valid backup.");
            return new RestoreResult(false, "Cancelled mid-restore. The database is in an unknown state.");
        }
        catch (Exception ex)
        {
            return new RestoreResult(false, $"Failed to launch pg_restore: {ex.Message}. Database may be in an unknown state.");
        }

        if (exitCode != 0)
        {
            log.Report($"[restore] pg_restore exited with code {exitCode}. Database may be partially restored.");
            return new RestoreResult(false, $"pg_restore failed (exit {exitCode}). Database may be in an unknown state — retry with another backup.");
        }

        log.Report("[restore] Restore completed successfully.");
        return new RestoreResult(true, "");
    }

    private static async Task<bool> RunPsqlAsync(
        ConnectionProfile profile,
        string targetDb,
        string sql,
        IDictionary<string, string> env,
        IProgress<string> log,
        CancellationToken ct)
    {
        var args = new[]
        {
            $"--host={profile.Host}",
            $"--port={profile.Port}",
            $"--username={profile.Username}",
            "--no-password",
            "--dbname=" + targetDb,
            "-c", sql,
        };

        try
        {
            var exitCode = await ProcessRunner.RunAsync("psql", args, env, log, ct);
            return exitCode == 0;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.Report($"[restore] psql error: {ex.Message}");
            return false;
        }
    }

    private static string EscapeDbName(string name) =>
        name.Replace("\"", "\"\"");
}
