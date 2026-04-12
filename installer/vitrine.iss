; Vitrine Inno Setup Installer Script
; Requires Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define AppName "Vitrine"
#define AppVersion "1.0.0"
#define AppPublisher "Emanuele Frascella"
#define AppURL "https://github.com"
#define AppExeName "Vitrine.exe"

[Setup]
AppId={{B7F1C3A0-4D2E-4F8B-9A1C-E5D6F7890ABC}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
OutputBaseFilename=VitrinevSetup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayName={#AppName}
; SetupIconFile=..\src\Vitrine.Engine\Assets\vitrine.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "autostart"; Description: "Start Vitrine when Windows starts"; GroupDescription: "Startup:"

[Files]
; Main executable
Source: "..\publish\release\Vitrine.exe"; DestDir: "{app}"; Flags: ignoreversion

; DLLs and runtime files
Source: "..\publish\release\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\release\*.xml"; DestDir: "{app}"; Flags: ignoreversion

; WebView2 user data folder
Source: "..\publish\release\Vitrine.exe.WebView2\*"; DestDir: "{app}\Vitrine.exe.WebView2"; Flags: ignoreversion recursesubdirs createallsubdirs

; Runtime native binaries
Source: "..\publish\release\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs

; Theme assets
Source: "..\publish\release\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start menu
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

; Desktop shortcut (optional)
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Auto-start on login (optional)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
; Launch after install
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill Vitrine before uninstalling
Filename: "taskkill"; Parameters: "/F /IM {#AppExeName}"; Flags: runhidden; RunOnceId: "KillVitrine"

[Code]
// Custom uninstall: ask to remove APPDATA
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDataDir := ExpandConstant('{userappdata}\Vitrine');
    if DirExists(AppDataDir) then
    begin
      if MsgBox('Do you want to remove all Vitrine data (themes, settings, logs)?'
        + #13#10 + #13#10 + AppDataDir,
        mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(AppDataDir, True, True, True);
      end;
    end;
  end;
end;
