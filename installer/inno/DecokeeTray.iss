#define AppName "Decokee Quake Tray"
#define AppVersion GetEnv("APP_VERSION")
#define AppPublisher "Decokee Tray Contributors"
#define AppExeName "decokee-tray.exe"
#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{2F66877F-1473-48AF-B63C-28923F5836D7}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\DecokeeTray
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\..\artifacts\installers
OutputBaseFilename=DecokeeTray-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

[Files]
Source: "..\..\artifacts\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
