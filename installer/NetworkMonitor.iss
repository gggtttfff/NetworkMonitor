#define MyAppName "NetworkMonitor"
#define MyAppPublisher "gggtttfff"
#define MyAppURL "https://github.com/gggtttfff/NetworkMonitor"
#define MyAppExeName "NetworkMonitor.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\\out\\publish\\win-x64"
#endif

[Setup]
AppId={{8D427BC0-4F05-46E5-B1D5-6A2C8D1D2B40}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\out\installer
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64os
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "appsettings.json"
Source: "{#PublishDir}\\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  RemoveDataOnUninstall: Boolean;

function InitializeUninstall(): Boolean;
begin
  RemoveDataOnUninstall :=
    MsgBox(
      '是否同时删除配置文件、日志和测试结果？' + #13#10 +
      '选择"是"将删除安装目录下 appsettings.json、logs、test_results 和 debug。',
      mbConfirmation,
      MB_YESNO or MB_DEFBUTTON2
    ) = IDYES;
  Result := True;
end;

procedure DeletePathIfExists(const APath: string);
begin
  if DirExists(APath) then
    DelTree(APath, True, True, True)
  else if FileExists(APath) then
    DeleteFile(APath);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDir: string;
begin
  if (CurUninstallStep = usUninstall) and RemoveDataOnUninstall then
  begin
    AppDir := ExpandConstant('{app}');
    DeletePathIfExists(ExpandConstant('{app}\appsettings.json'));
    DeletePathIfExists(ExpandConstant('{app}\logs'));
    DeletePathIfExists(ExpandConstant('{app}\test_results'));
    DeletePathIfExists(ExpandConstant('{app}\debug'));

    if DirExists(AppDir) then
      RemoveDir(AppDir);
  end;
end;
