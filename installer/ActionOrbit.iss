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
#ifdef ArtworkDir
WizardImageFile={#ArtworkDir}\wizard.bmp
WizardSmallImageFile={#ArtworkDir}\wizard-small.bmp
WizardImageBackColor=$16110F
#endif
UninstallDisplayIcon={app}\ActionOrbit.App.exe
LicenseFile=..\LICENSE
WizardStyle=modern
DisableWelcomePage=no
DisableDirPage=no
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
Name: "startup"; Description: "Windows ile birlikte başlat (bildirim alanında)"; GroupDescription: "Başlangıç:"; Flags: unchecked
Name: "startmenuicon"; Description: "Başlat menüsüne kısayol ekle"; GroupDescription: "Kısayollar:"
Name: "desktopicon"; Description: "Masaüstüne kısayol ekle"; GroupDescription: "Kısayollar:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,config*.json,shelves.json,orbit-link*.json,*.log,logs\*,shelf-cache\*"
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Action Orbit Pro"; Filename: "{app}\ActionOrbit.App.exe"; WorkingDir: "{app}"; Tasks: startmenuicon
Name: "{userdesktop}\Action Orbit Pro"; Filename: "{app}\ActionOrbit.App.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\ActionOrbit.App.exe"; Description: "{cm:LaunchProgram,Action Orbit Pro}"; Flags: nowait postinstall skipifsilent

[Code]
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel1.Caption := 'Action Orbit Pro''ya hoş geldin';
  WizardForm.WelcomeLabel2.Caption := 'Sık kullandığın işlemleri tek bir halkada topla.' + #13#10#13#10 +
    'Bir sonraki adımda kurulum klasörünü, Windows ile başlatmayı ve kısayolları seçebilirsin.' + #13#10#13#10 +
    'Mevcut profillerin ve Orbit Shelf içeriklerin korunur. Yönetici izni veya ek .NET kurulumu gerekmez.';
  { Optional placement for multi-monitor UI verification. }
  if ExpandConstant('{param:UXLEFT|}') <> '' then begin
    WizardForm.Position := poDesigned;
    WizardForm.Left := StrToIntDef(ExpandConstant('{param:UXLEFT|0}'), 0);
    WizardForm.Top := StrToIntDef(ExpandConstant('{param:UXTOP|100}'), 100);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  StartupEnabled: Cardinal;
begin
  if CurStep = ssPostInstall then begin
    StartupEnabled := 0;
    if WizardIsTaskSelected('startup') then begin
      StartupEnabled := 1;
      RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro',
        '"' + ExpandConstant('{app}\ActionOrbit.App.exe') + '" --background');
    end else
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro');
    { Consume once on first app launch, including when launch-after-install is unchecked. }
    RegWriteDWordValue(HKCU, 'Software\ActionOrbitPro\Setup', 'RunAtStartup', StartupEnabled);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro', StartupCommand) then
      if (CompareText(StartupCommand, '"' + ExpandConstant('{app}\ActionOrbit.App.exe') + '"') = 0) or
         (CompareText(StartupCommand, '"' + ExpandConstant('{app}\ActionOrbit.App.exe') + '" --background') = 0) then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro');
  if CurUninstallStep = usUninstall then begin
    RegDeleteValue(HKCU, 'Software\ActionOrbitPro\Setup', 'RunAtStartup');
    RegDeleteKeyIfEmpty(HKCU, 'Software\ActionOrbitPro\Setup');
  end;
end;
