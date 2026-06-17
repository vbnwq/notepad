; ============================================================================
;  PersiaPad - Inno Setup installer script
;  Builds a single setup.exe that installs PersiaPad and registers it as a
;  selectable default text editor (per-user, no admin needed).
;
;  How to build the installer:
;    1) Publish the app:   build\publish.bat   (creates build\publish\)
;    2) Install Inno Setup: https://jrsoftware.org/isdl.php
;    3) Open this .iss in Inno Setup Compiler and press Build,
;       OR run:  ISCC.exe installer\PersiaPad.iss
;  Output: installer\Output\PersiaPad-Setup.exe
; ============================================================================

#define AppName "PersiaPad"
#define AppVersion "1.0.0"
#define AppPublisher "PersiaPad"
#define AppExeName "PersiaPad.exe"

[Setup]
AppId={{B7A1F1E2-8C3D-4F6A-9E10-PERSIAPAD0001}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Per-user install => no admin prompt, works on locked-down/weak machines.
PrivilegesRequiredOverridesAllowed=dialog
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=PersiaPad-Setup
SetupIconFile=..\src\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
; Persian language file ships with newer Inno Setup; comment out if missing.
; Name: "persian"; MessagesFile: "compiler:Languages\Persian.isl"

[Tasks]
; Desktop icon is checked by default so the user always gets a desktop shortcut.
Name: "desktopicon"; Description: "ایجاد میانبر روی دسکتاپ"; GroupDescription: "میانبرها:"; Flags: checkedonce
Name: "associatetxt"; Description: "باز کردن فایل‌های .txt با PersiaPad"; GroupDescription: "نوع فایل:"; Flags: checkedonce

[Files]
; Copy everything produced by the publish step.
Source: "..\build\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\حذف {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; --- ProgId: how PersiaPad opens a document -------------------------------
Root: HKCU; Subkey: "Software\Classes\PersiaPad.txt"; ValueType: string; ValueName: ""; ValueData: "PersiaPad Document"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\PersiaPad.txt\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppExeName},0"
Root: HKCU; Subkey: "Software\Classes\PersiaPad.txt\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""

; --- App capabilities so Windows "Default apps" lists PersiaPad -----------
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "PersiaPad"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "ویرایشگر متن سبک با پشتیبانی کامل فارسی"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".txt"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".log"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".md"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".json"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".css"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".html"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\PersiaPad\Capabilities\FileAssociations"; ValueType: string; ValueName: ".xml"; ValueData: "PersiaPad.txt"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "PersiaPad"; ValueData: "Software\PersiaPad\Capabilities"; Flags: uninsdeletevalue

; --- Add to "Open with" list for .txt (optional task) ---------------------
Root: HKCU; Subkey: "Software\Classes\.txt\OpenWithProgids"; ValueType: none; ValueName: "PersiaPad.txt"; Flags: uninsdeletevalue; Tasks: associatetxt

[Run]
Filename: "{app}\{#AppExeName}"; Description: "اجرای PersiaPad"; Flags: nowait postinstall skipifsilent
