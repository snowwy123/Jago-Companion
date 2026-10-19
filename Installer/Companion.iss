#ifndef Payload
#error Define Payload to the prepared Companion files
#endif
#ifndef Output
#define Output "Output"
#endif
[Setup]
AppId=JagoCompanionPreview
AppName=Jago Companion Preview
AppVersion=0.7.3
AppPublisher=Cameron Jago Lis Illustrates
AppPublisherURL=https://jagosuite.co.uk/
DefaultDirName={localappdata}\Programs\Jago Companion Preview
DefaultGroupName=Jago Companion Preview
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
DisableWelcomePage=no
UsePreviousTasks=yes
UsePreviousAppDir=yes
WizardStyle=modern light
WizardSizePercent=120
WizardImageFile=Brand\wizard.bmp
WizardSmallImageFile=Brand\small.bmp
WizardImageStretch=yes
WizardImageBackColor=$F0F6F8
WizardBackColor=$F0F6F8
SetupIconFile=..\Brand\Companion.ico
UninstallDisplayIcon={app}\Jago-Companion.exe
OutputDir={#Output}
OutputBaseFilename=Jago-Companion-Setup-Preview
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no
[Tasks]
Name: capturedesktop; Description: "Add a one-click Jago Capture desktop shortcut"; Flags: unchecked
Name: desktop; Description: "Add a desktop shortcut"; Flags: unchecked
Name: startup; Description: "Start Companion quietly when I sign in (quick notes and capture shortcuts)"; Flags: unchecked
[Files]
Source: "{#Payload}\Jago-Preview.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\Jago-Update.exe"; Flags: dontcopy
Source: "{#Payload}\Jago-Update.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\Jago-Recorder.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\ScreenRecorderLib.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\Notices\*"; DestDir: "{app}\Notices"; Flags: recursesubdirs createallsubdirs
Source: "{#Payload}\Jago-Capture.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\Jago-Companion.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#Payload}\README.md"; DestDir: "{app}"
Source: "{#Payload}\LICENSE.txt"; DestDir: "{app}"
Source: "{#Payload}\Documentation\*"; DestDir: "{app}\Documentation"; Flags: recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Jago Capture"; Filename: "{app}\Jago-Capture.exe"; AppUserModelID: "Jago.Capture"
Name: "{group}\Jago Companion"; Filename: "{app}\Jago-Companion.exe"
Name: "{group}\Uninstall Jago Companion Preview"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Jago Companion Preview"; Filename: "{app}\Jago-Companion.exe"; Tasks: desktop
Name: "{userdesktop}\Jago Capture Preview"; Filename: "{app}\Jago-Capture.exe"; Tasks: capturedesktop; AppUserModelID: "Jago.Capture"
[Registry]
Root: HKCU; Subkey: "Software\Jago Suite\Preview\Components\companion"; ValueType: string; ValueName: "Executable"; ValueData: "{app}\Jago-Companion.exe"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "JagoCompanionPreview"; ValueData: """{app}\Jago-Companion.exe"" --tray"; Tasks: startup
[Run]
Filename: "{app}\Jago-Capture.exe"; Parameters: "--register"; Flags: runhidden waituntilterminated
Filename: "{app}\Jago-Companion.exe"; Description: "Open Jago Companion"; Flags: nowait postinstall skipifsilent
[Code]
procedure InitializeWizard();
var I: Integer;
begin
 WizardForm.Font.Name := 'Segoe UI';
 WizardForm.Color := $F0F6F8;
 WizardForm.TasksList.Color := $F0F6F8;
 WizardForm.TasksList.Font.Color := $3A3627;
 for I := 0 to WizardForm.ComponentCount-1 do
  if WizardForm.Components[I] is TNewStaticText then
   TNewStaticText(WizardForm.Components[I]).Font.Color := $3A3627;
 WizardForm.PageNameLabel.Font.Color := $6E6D48;
 WizardForm.WelcomeLabel1.Font.Color := $6E6D48;
 WizardForm.WelcomeLabel1.Caption := 'Catch an idea. Keep it moving.';
 WizardForm.WelcomeLabel2.Caption := 'Quick notes, floating task lists and local screen capture. Companion works independently or beside your studios in Workscape.' + #13#10#13#10 + 'Notes and captures live in Documents\Jago\Companion by default. You can choose another home in Settings. Your files and preferences are kept when you update or uninstall.';
end;
procedure RegisterExtraCloseApplicationsResources();
begin
 RegisterExtraCloseApplicationsResource(ExpandConstant('{app}\Jago-Companion.exe'));
 RegisterExtraCloseApplicationsResource(ExpandConstant('{app}\Jago-Recorder.exe'));
 RegisterExtraCloseApplicationsResource(ExpandConstant('{app}\Jago-Preview.exe'));
 RegisterExtraCloseApplicationsResource(ExpandConstant('{app}\Jago-Capture.exe'));
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
var Existing, HelperArgs: String; Code: Integer;
begin
 Result := '';
 if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\JagoCompanionPreview_is1', 'InstallLocation', Existing) then
  if DirExists(Existing) and (CompareText(RemoveBackslashUnlessRoot(Existing), RemoveBackslashUnlessRoot(ExpandConstant('{app}'))) <> 0) then
   Result := 'Companion is already installed at '+Existing+'. Update it there, or uninstall before changing the program location. Your personal files are kept.';
 if Result <> '' then exit;
 ExtractTemporaryFile('Jago-Update.exe');
 HelperArgs := '--app '+Chr(34)+ExpandConstant('{app}\Jago-Companion.exe')+Chr(34);
 if WizardSilent then HelperArgs := HelperArgs+' --quiet';
 if not Exec(ExpandConstant('{tmp}\Jago-Update.exe'), HelperArgs, '', SW_SHOWNORMAL, ewWaitUntilTerminated, Code) or (Code <> 0) then
  Result := 'Close Companion using Quit from its tray menu, then retry. Your files have not been replaced.';
end;
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Value: String;
begin
 if CurUninstallStep <> usUninstall then exit;
 if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'JagoCompanionPreview', Value) then
  if Pos(Lowercase(ExpandConstant('{app}\Jago-Companion.exe')), Lowercase(Value)) > 0 then
   RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'JagoCompanionPreview');
end;
