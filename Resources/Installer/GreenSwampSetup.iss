; Script generated by the ASCOM Driver Installer Script Generator 6.2.0.0
; Generated by Robert Morgan on 5/20/2018 (UTC)
#define MyAppVersion "1.0.4.0"
#define ManualName "GSS Manual v1040.pdf"
#define VersionNumber "v1040"
#define InstallerBaseName "ASCOMGSServer1040Setup"
#define MyAppName "GSServer"
#define MyAppExeName "GS.Server.exe"

[Setup]
AppVerName=ASCOM GS Server {#MyAppVersion}
AppVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
OutputBaseFilename={#InstallerBaseName}
AppID={{0ff78bd6-6149-4536-9252-3da68b94f7c2}
AppName=GS Server
AppPublisher=Robert Morgan <robert.morgan.e@gmail.com>
AppPublisherURL=mailto:robert.morgan.e@gmail.com
AppSupportURL=https://ascomtalk.groups.io/g/Developer/topics
AppUpdatesURL=http://ascom-standards.org/
MinVersion=0,6.1
DefaultDirName="{cf}\ASCOM\Telescope\GSServer"
DefaultGroupName="GS Server"
DisableDirPage=yes
DisableProgramGroupPage=yes
OutputDir="."
Compression=lzma
SetupIconFile="greenswamp2.ico"       
SetupLogging=yes
SolidCompression=yes
; Put there by Platform if Driver Installer Support selected
WizardImageFile="WizardImage1.bmp"
LicenseFile="License.txt"
; {cf}\ASCOM\Uninstall\Telescope folder created by Platform, always
UninstallFilesDir="{cf}\ASCOM\Uninstall\Telescope\GSServer"
;"C:\Program Files (x86)\Windows Kits\10\Tools\bin\i386\signtool.exe" sign /f "C:\Users\Rob\source\repos\GSSolution\Resources\Installer\GreenSwamp.pfx" /p rem /d "GreenSwamp Installer"  $f
;"C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x86\signtool.exe" sign /f "C:\Users\phil\source\repos\GSServer\Resources\Installer\GreenSwamp.pfx" /p rem /d "GreenSwamp Installer" $f
;SignTool=Signtool
DisableWelcomePage=No 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Dirs]
Name: "{cf}\ASCOM\Uninstall\Telescope\GSServer\"
Name: "{cf}\ASCOM\Uninstall\Telescope\GSServer\SkyScripts\"
Name: "{cf}\ASCOM\Uninstall\Telescope\GSServer\Notes\NotesTemplates\"
Name: "{cf}\ASCOM\Uninstall\Telescope\GSServer\Models"
Name: "{cf}\ASCOM\Uninstall\Telescope\GSServer\LanguageFiles"
; TODO: Add subfolders below {app} as needed (e.g. Name: "{app}\MyFolder")

[Files]
Source: "..\..\Builds\Release\*.*"; DestDir: "{app}"
Source: "..\..\Builds\Release\SkyScripts\*.*"; DestDir: "{app}\SkyScripts";
Source: "..\..\Builds\Release\Notes\NotesTemplates\*.*"; DestDir: "{app}\NotesTemplates";
Source: "..\..\Builds\Release\Models\*.*"; DestDir: "{app}\Models";
Source: "..\..\Builds\Release\LanguageFiles\*.*"; DestDir: "{app}\LanguageFiles";
; Require a read-me to appear after installation, maybe driver's Help doc
Source: "..\Manuals\GSS Manual.pdf"; DestDir: "{app}"; DestName:"{#ManualName}"; Flags: isreadme
; TODO: Add other files needed by your driver here (add subfolders above)

[Languages]
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Messages]
en.WelcomeLabel2={#MyAppName} {#MyAppVersion}%n%nThis will install {#MyAppName} {#MyAppVersion} on your computer.%n%nIt is recommended that you close all other applications that may be currently using {#MyAppName}
fr.WelcomeLabel2={#MyAppName} {#MyAppVersion}%n%nCela installera {#MyAppName} {#MyAppVersion} sur votre ordinateur.%n%nIl est recommandé de fermer toutes les autres applications qui utilisent actuellement {#MyAppName}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Icons]
Name: "{group}\GS Server {#VersionNumber}"; Filename: "{app}\GS.Server.exe"
Name: "{group}\GS Chart Viewer {#VersionNumber}"; Filename: "{app}\GS.ChartViewer.exe"
Name: "{group}\GS Utilities {#VersionNumber}"; Filename: "{app}\GS.Utilities.exe"
Name: "{group}\GS Manual {#VersionNumber}"; Filename: "{app}\{#ManualName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; Only if driver is .NET
[Run]

; Only for .NET local-server drivers
Filename: "{app}\GS.Server.exe"; Parameters: "/register /{language}"

; Only if driver is .NET
[UninstallRun]
; This helps to give a clean uninstall

; Only for .NET local-server drivers, use /unprofile to remove ascom profile 
Filename: "{app}\GS.Server.exe"; Parameters: "/unregister /unprofile"

[Code]
const
   REQUIRED_PLATFORM_VERSION = 6.2;    // Set this to the minimum required ASCOM Platform version for this application

procedure InitializeWizard();
begin
  //WizardForm.WelcomeLabel2.Font.Style := [fsBold]; //Bold
  WizardForm.WelcomeLabel2.Font.Color := clRed; // And red colour
  WizardForm.WelcomeLabel2.Font.Size  := 10;
end;

//
// Function to return the ASCOM Platform's version number as a double.
//
function PlatformVersion(): Double;
var
   PlatVerString : String;
begin
   Result := 0.0;  // Initialise the return value in case we can't read the registry
   try
      if RegQueryStringValue(HKEY_LOCAL_MACHINE_32, 'Software\ASCOM','PlatformVersion', PlatVerString) then 
      begin // Successfully read the value from the registry
         Result := StrToFloat(PlatVerString); // Create a double from the X.Y Platform version string
      end;
   except                                                                   
      ShowExceptionMessage;
      Result:= -1.0; // Indicate in the return value that an exception was generated
   end;
end;

//
// Before the installer UI appears, verify that the required ASCOM Platform version is installed.
//
function InitializeSetup(): Boolean;
var
   PlatformVersionNumber : double;
 begin
   Result := FALSE;  // Assume failure
   PlatformVersionNumber := PlatformVersion(); // Get the installed Platform version as a double
   If PlatformVersionNumber >= REQUIRED_PLATFORM_VERSION then	// Check whether we have the minimum required Platform or newer
      Result := TRUE
   else
      if PlatformVersionNumber = 0.0 then
         MsgBox('No ASCOM Platform is installed. Please install Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later from https://www.ascom-standards.org', mbCriticalError, MB_OK)
      else 
         MsgBox('ASCOM Platform ' + Format('%3.1f', [REQUIRED_PLATFORM_VERSION]) + ' or later is required, but Platform '+ Format('%3.1f', [PlatformVersionNumber]) + ' is installed. Please install the latest Platform before continuing; you will find it at https://www.ascom-standards.org', mbCriticalError, MB_OK);
end;

// Code to enable the installer to uninstall previous versions of itself when a new version is installed
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  UninstallExe: String;
  UninstallRegistry: String;
  logfilepathname, logfilename, newfilepathname: string;
begin
  if (CurStep = ssInstall) then // Install step has started
	begin
      // Create the correct registry location name, which is based on the AppId
      UninstallRegistry := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}' + '_is1');
      // Check whether an extry exists
      if RegQueryStringValue(HKLM, UninstallRegistry, 'UninstallString', UninstallExe) then
        begin // Entry exists and previous version is installed so run its uninstaller quietly after informing the user
          if ActiveLanguage = 'en' then
          begin
            MsgBox('Setup will now remove the previous version.', mbInformation, MB_OK);
          end;
          if ActiveLanguage = 'fr' then
          begin
            MsgBox('Le programme d''installation supprimera désormais la version précédente.', mbInformation, MB_OK);
          end;
          Exec(RemoveQuotes(UninstallExe), ' /SILENT', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
          sleep(1000);    //Give enough time for the install screen to be repainted before continuing
        end;
  end;
  // copy install log to the app folder
  if CurStep = ssDone then
  begin
    logfilepathname := ExpandConstant('{log}');
    logfilename := ExtractFileName(logfilepathname);
    newfilepathname := ExpandConstant('{app}\') + logfilename;
    FileCopy(logfilepathname, newfilepathname, false);
  end;
end;