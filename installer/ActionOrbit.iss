#ifndef PublishDir
  #error PublishDir must point to a self-contained Windows x64 publish directory.
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\setup"
#endif
#ifndef AppVersion
  #define AppVersion "2.0.0"
#endif
#ifndef BuildId
  #define BuildId "local"
#endif

[Setup]
AppId={{5F828BC8-56F7-4538-9B7B-05818EB27780}
AppName=Action Orbit Pro
AppVersion={#AppVersion}
AppPublisher=Action Orbit contributors
AppPublisherURL=https://github.com/SabahattinAkal/action-orbit
DefaultDirName={localappdata}\Programs\Action Orbit Pro
DefaultGroupName=Action Orbit Pro
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#OutputDir}
OutputBaseFilename=ActionOrbitPro-{#AppVersion}-{#BuildId}-Setup-win-x64
SetupIconFile=..\src\ActionOrbit.App\Assets\Brand\ActionOrbit.ico
UninstallDisplayIcon={app}\ActionOrbit.App.exe
LicenseFile=..\LICENSE
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=ActionOrbit.App.exe
RestartApplications=no
DisableProgramGroupPage=yes
UninstallDisplayName=Action Orbit Pro
VersionInfoProductName=Action Orbit Pro
VersionInfoVersion={#AppVersion}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,config*.json,shelves.json,orbit-link*.json,*.log,logs\*,shelf-cache\*"
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Action Orbit Pro"; Filename: "{app}\ActionOrbit.App.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\Action Orbit Pro"; Filename: "{app}\ActionOrbit.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\ActionOrbit.App.exe"; Description: "{cm:LaunchProgram,Action Orbit Pro}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro', StartupCommand) then
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\ActionOrbit.App.exe') + '"') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro');
end;
