using System.Diagnostics;

namespace PgBackupTool.Services;

public static class ProcessRunner
{
    public static async Task<int> RunAsync(
        string fileName,
        IEnumerable<string> args,
        IDictionary<string, string>? envVars,
        IProgress<string>? log,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        if (envVars != null)
            foreach (var kv in envVars)
                psi.Environment[kv.Key] = kv.Value;

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) => { if (e.Data != null) log?.Report(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) log?.Report(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var registration = ct.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }
        });

        await using (registration)
        {
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
        }

        return process.ExitCode;
    }
}
