namespace PgBackupTool.Models;

public class BackupEntry
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ProfileName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string SizeDisplay => FormatSize(SizeBytes);

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024) return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }
}
