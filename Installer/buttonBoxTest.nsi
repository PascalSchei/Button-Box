Unicode true
!define APP "Button Box"
!define VERSION "1.2.0"
!define PUBLISH "..\ButtonBoxBlazor\bin\Publish"
!define ASPNET_URL "https://aka.ms/dotnet/10.0/aspnetcore-runtime-win-x64.exe"
!define NETCORE_URL "https://aka.ms/dotnet/10.0/dotnet-runtime-win-x64.exe"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\ButtonBox"

!include MUI2.nsh
!include LogicLib.nsh

Name "${APP}"
OutFile "ButtonBox-Setup-Test-${VERSION}.exe"
InstallDir "$PROGRAMFILES64\ButtonBoxCondor"
InstallDirRegKey HKLM "Software\ButtonBox" "InstallDir"
RequestExecutionLevel admin

!define MUI_PAGE_HEADER_TEXT "Additional options"
!define MUI_PAGE_HEADER_SUBTEXT "Choose optional shortcuts."
!define MUI_COMPONENTSPAGE_TEXT_TOP "Select the optional items you want to create, then click Install."
!define MUI_COMPONENTSPAGE_TEXT_COMPLIST "Optional items:"
!define MUI_FINISHPAGE_RUN "$INSTDIR\ButtonBoxBlazor.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Start ${APP}"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  SetShellVarContext all
  SetRegView 64
FunctionEnd

Function un.onInit
  SetShellVarContext all
  SetRegView 64
FunctionEnd

; In: $R0 = shared framework name. Result in $0: 1 = version 10.0.x registered, 0 = missing
Function CheckFramework10
  StrCpy $0 0
  StrCpy $1 0
  loop:
    EnumRegValue $2 HKLM "SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\$R0" $1
    StrCmp $2 "" done
    StrCpy $3 $2 5
    StrCmp $3 "10.0." 0 next
      StrCpy $0 1
      Goto done
    next:
    IntOp $1 $1 + 1
    Goto loop
  done:
FunctionEnd

; In: $R1 = installer URL, $R2 = display name. Failures are only reported; the installation continues.
Function InstallRuntime
  InitPluginsDir
  DetailPrint "Downloading $R2..."
  ; Force TLS 1.2 (Windows PowerShell 5.1 may default to older protocols) and print the error text to the details log
  nsExec::ExecToLog 'powershell -NoProfile -ExecutionPolicy Bypass -Command "try { [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; $$ProgressPreference=\"SilentlyContinue\"; Invoke-WebRequest -UseBasicParsing -Uri $R1 -OutFile \"$PLUGINSDIR\runtime.exe\" } catch { Write-Host $$_.Exception.Message; exit 1 }"'
  Pop $0
  ${If} $0 != 0
    DetailPrint "Download of $R2 failed. Continuing without it."
    MessageBox MB_ICONEXCLAMATION|MB_OK "Download failed. The installation continues, but please install $R2 manually:$\n$R1"
    Return
  ${EndIf}
  DetailPrint "Installing $R2..."
  ExecWait '"$PLUGINSDIR\runtime.exe" /install /quiet /norestart' $0
  ${If} $0 != 0
  ${AndIf} $0 != 3010
    DetailPrint "$R2 installation failed (exit code $0). Continuing without it."
    MessageBox MB_ICONEXCLAMATION|MB_OK "$R2 installation failed (exit code $0).$\nThe installation continues, but please install it manually:$\n$R1"
  ${EndIf}
FunctionEnd

; Sections starting with "-" are hidden on the components page and always run
Section "-.NET Runtime 10"
  StrCpy $R0 "Microsoft.NETCore.App"
  Call CheckFramework10
  ${If} $0 == 0
    StrCpy $R1 "${NETCORE_URL}"
    StrCpy $R2 ".NET Runtime 10"
    Call InstallRuntime
  ${EndIf}
SectionEnd

Section "-ASP.NET Core Runtime 10"
  StrCpy $R0 "Microsoft.AspNetCore.App"
  Call CheckFramework10
  ${If} $0 == 0
    StrCpy $R1 "${ASPNET_URL}"
    StrCpy $R2 "ASP.NET Core Runtime 10"
    Call InstallRuntime
  ${EndIf}
SectionEnd

Section "-Button Box"
  SetOutPath "$INSTDIR"
  File /r "${PUBLISH}\*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\${APP}"
  CreateShortcut "$SMPROGRAMS\${APP}\${APP}.lnk" "$INSTDIR\ButtonBoxBlazor.exe"

  WriteRegStr HKLM "Software\ButtonBox" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${APP}"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
SectionEnd

; Selected by default; the user can untick it on the components page
Section "Desktop shortcut" SecDesktop
  SetOutPath "$INSTDIR"
  CreateShortcut "$DESKTOP\${APP}.lnk" "$INSTDIR\ButtonBoxBlazor.exe"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\${APP}.lnk"
  RMDir /r "$SMPROGRAMS\${APP}"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "${UNINST_KEY}"
  DeleteRegKey HKLM "Software\ButtonBox"
SectionEnd
