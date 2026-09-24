#define MyAppName "QSentinel"
#define MyAppVersion "1.3.0"
#define MyAppPublisher "MukaSanches"
#define MyAppExeName "QSentinel.exe"

[Setup]
AppId={{9CB87EB6-CF19-4EA4-A2EA-171744160011}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\QSentinel
DefaultGroupName=QSentinel
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=QSentinel-Setup-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Files]
Source: "..\publish\QSentinel.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\QSentinel"; Filename: "{app}\QSentinel.exe"
Name: "{autodesktop}\QSentinel"; Filename: "{app}\QSentinel.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "QSentinel"; ValueData: """{app}\QSentinel.exe"" --background"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\QSentinel.exe"; Description: "Abrir QSentinel"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /F /IM QSentinel.exe"; Flags: runhidden waituntilterminated; RunOnceId: "StopQSentinel"
