# PgBackupTool

A lightweight Windows desktop application for managing PostgreSQL backups and restores. Thin GUI wrapper around `pg_dump`, `pg_restore`, and `psql` — no direct DB driver, no background jobs.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL client tools 14+ (`pg_dump`, `pg_restore`, `psql`) on `PATH`
  - Installed automatically with PostgreSQL, or via [EDB installers](https://www.enterprisedb.com/downloads/postgres-postgresql-downloads)
  - Verify: `pg_dump --version`

## Build

```
dotnet build PgBackupTool
```

## Run

```
dotnet run --project PgBackupTool
```

Or open `PgBackupTool.slnx` in Visual Studio 2022+.

## Publish as a standalone EXE

The commands below produce a single self-contained executable that runs on any Windows machine **without requiring .NET to be installed**.

### Single-file, self-contained (recommended for distribution)

```
dotnet publish PgBackupTool -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/win-x64
```

Output: `publish/win-x64/PgBackupTool.exe` (~130 MB, no dependencies).

> **Note:** PostgreSQL client tools (`pg_dump` etc.) still need to be on `PATH` on the target machine — they are not bundled.

### Framework-dependent (smaller, requires .NET 10 Runtime on target)

```
dotnet publish PgBackupTool -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish/win-x64-fdd
```

Output: ~3 MB. Target machine needs [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

---

## Create an installer

### Option A — Inno Setup (recommended, free)

1. Download and install [Inno Setup 6](https://jrsoftware.org/isdl.php).
2. Use the `installer.iss`
3. Publish the exe first, then compile the script:

```
dotnet publish PgBackupTool -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

Output: `Output\PgBackupToolSetup.exe` — a standard Windows installer wizard.

## Data storage

All app data is stored under `%APPDATA%\PgBackupTool\`:

```
%APPDATA%\PgBackupTool\
├── profiles.json          # Connection profiles
└── backups\
    └── <profile-name>\
        └── <profile-name>_<yyyyMMdd_HHmmss>.dump
```

## Usage

1. **Add a profile** — click `+ Add` in the left panel, fill in connection details.
2. **Backup** — select a profile, click `Backup Now`. Progress streams live in the log panel.
3. **Restore** — select a row in the history grid, click `Restore`. A confirmation dialog warns that the target database will be permanently dropped and recreated.
4. **Cancel** — a `Cancel` button appears during any running operation. Mid-backup cancellation deletes the partial file. Mid-restore cancellation leaves the database in an unknown state (clearly logged).

## Known limitations

- **Plain-text passwords** — stored unencrypted in `profiles.json`. A `// TODO: encrypt with DPAPI` marker is in `ConnectionProfile.cs`. Do not use on shared machines without addressing this first.
- **Windows-only** — WPF requires Windows.
- **Custom-format backups only** — `pg_dump --format=custom`. These are not human-readable but give the best compression and selective-restore capability via `pg_restore`.
- **No scheduling** — all operations are user-initiated.
- **`postgres` maintenance DB assumed** — restore logic connects to the `postgres` database to drop/create the target. If your server doesn't have a `postgres` database, adjust `RestoreService.cs`.
