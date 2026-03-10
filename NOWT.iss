[Setup]
#define MyAppSetupName 'YAWN'
#define MyAppVersion '1.3.8'
#define MyAppPublisher 'PWall'
#define MyAppCopyright 'Soneliem & PWall'
#define MyAppURL 'https://github.com/pwall2222/NOWT'

AppName={#MyAppSetupName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppSetupName} {#MyAppVersion}
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
OutputBaseFilename={#MyAppSetupName}
DefaultGroupName={#MyAppSetupName}
DefaultDirName={autopf}\{#MyAppSetupName}
UninstallDisplayIcon=C:\Users\Coder\Desktop\NOWT\NOWT\logo.ico
SetupIconFile=C:\Users\Coder\Desktop\NOWT\NOWT\logo.ico
SourceDir=inno
OutputDir=out
AllowNoIcons=yes
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; remove next line if you only deploy 32-bit binaries and dependencies
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"

[Files]
; #ifdef UseNetCoreCheck
; download netcorecheck.exe: https://go.microsoft.com/fwlink/?linkid=2135256
; download netcorecheck_x64.exe: https://go.microsoft.com/fwlink/?linkid=2135504
;Source: "netcorecheck.exe"; Flags: dontcopy noencryption
;Source: "netcorecheck_x64.exe"; Flags: dontcopy noencryption
; #endif

;#ifdef UseDirectX
;Source: "dxwebsetup.exe"; Flags: dontcopy noencryption
;#endif

Source: "YAWN.exe"; DestDir: "{app}"; DestName: "YAWN.exe"; Flags: ignoreversion
Source: "*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "*.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs
[Icons]
Name: "{group}\{#MyAppSetupName}"; Filename: "{app}\YAWN.exe"
Name: "{group}\{cm:UninstallProgram,{#MyAppSetupName}}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppSetupName}"; Filename: "{app}\YAWN.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Run]
Filename: "{app}\YAWN.exe"; Description: "{cm:LaunchProgram,{#MyAppSetupName}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
function InitializeSetup: Boolean;
begin
  Result := True;
end;
