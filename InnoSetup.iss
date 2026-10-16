; Build the app first (from the repo root):
;   dotnet publish LightroomSync/LightroomSync.csproj -c Release -r win-x64 --self-contained -o publish
; then compile this script (ISCC InnoSetup.iss). The installer lands in the "installer" folder.

#define MyAppName "LightroomSync"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Anthony Bryan"
#define MyAppURL "https://github.com/software-2/LightroomSync"
#define MyAppExeName "LightroomSync.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Keep it so new versions upgrade existing installs.
AppId={{E517626F-1634-44A5-A111-1BB07C55E499}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
SetupIconFile=LightroomSync\camera.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
; Per-user install by default (no admin prompt); the user can still choose all users in the dialog.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=installer
OutputBaseFilename=LightroomSync Setup {#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; Close a running copy so its files can be replaced during an upgrade.
CloseApplications=force
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained publish output: the .NET runtime is included, nothing else to install.
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
