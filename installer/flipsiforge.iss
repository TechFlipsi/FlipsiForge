; FlipsiForge Inno Setup Script v0.2.0
; TechFlipsi (Fabian Kirchweger) — GPL-3.0
; Kompilieren mit: iscc flipsiforge.iss (Inno Setup 6+)

#define MyAppName "FlipsiForge"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "TechFlipsi (Fabian Kirchweger)"
#define MyAppURL "https://techflipsi.at"
#define MyAppExeName "FlipsiForge.Desktop.exe"

[Setup]
AppId={{FlipsiForge-TechFlipsi-2026}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL=https://github.com/TechFlipsi/FlipsiForge
DefaultDirName={autopf}\FlipsiForge
DefaultGroupName=FlipsiForge
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=FlipsiForge-0.2.0-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
LicenseFile=LICENSE

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "dist\win-desktop\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent