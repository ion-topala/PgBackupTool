[Setup]
AppName=PgBackupTool
AppVersion=1.1
DefaultDirName={autopf}\PgBackupTool
DefaultGroupName=PgBackupTool
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=PgBackupTool\Assets\logo.ico
OutputDir=publish
OutputBaseFilename=PgBackupToolSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "publish/win-x64/*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\PgBackupTool"; Filename: "{app}\PgBackupTool.exe"
Name: "{commondesktop}\PgBackupTool"; Filename: "{app}\PgBackupTool.exe"

[Run]
Filename: "{app}\PgBackupTool.exe"; Description: "Launch PgBackupTool"; Flags: nowait postinstall skipifsilent
