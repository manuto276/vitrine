; Vitrine Installer — NSIS script
; Builds a per-user Windows 11 x64 installer (no admin required)

!include "MUI2.nsh"
!include "FileFunc.nsh"

; ---------------------------------------------------------------------------
; Build-time defines (can be overridden via makensis -D)
; ---------------------------------------------------------------------------
!ifndef VERSION
  !define VERSION "1.0.0"
!endif

!ifndef SOURCE_DIR
  !define SOURCE_DIR "..\publish\release"
!endif

!ifndef OUTPUT_DIR
  !define OUTPUT_DIR "..\publish\installer"
!endif

; ---------------------------------------------------------------------------
; Installer metadata
; ---------------------------------------------------------------------------
Name "Vitrine ${VERSION}"
OutFile "${OUTPUT_DIR}\VitrineSetup-${VERSION}.exe"
InstallDir "$LOCALAPPDATA\Programs\Vitrine"
InstallDirRegKey HKCU "Software\Vitrine" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma

; ---------------------------------------------------------------------------
; Version info embedded in the EXE
; ---------------------------------------------------------------------------
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "Vitrine"
VIAddVersionKey "ProductVersion" "${VERSION}"
VIAddVersionKey "FileVersion" "${VERSION}.0"
VIAddVersionKey "FileDescription" "Vitrine Installer"
VIAddVersionKey "LegalCopyright" "Vitrine contributors"

; ---------------------------------------------------------------------------
; Modern UI configuration
; ---------------------------------------------------------------------------
!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\Vitrine.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Launch Vitrine"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Italian"

; ---------------------------------------------------------------------------
; Installer sections
; ---------------------------------------------------------------------------
Section "Vitrine (required)" SecCore
  SectionIn RO

  ; Kill running instance before overwriting
  nsExec::ExecToLog 'taskkill /F /IM Vitrine.exe'

  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*.*"

  ; Store install dir in registry
  WriteRegStr HKCU "Software\Vitrine" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Vitrine" "Version" "${VERSION}"

  ; Uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  ; Add/Remove Programs entry
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "DisplayName" "Vitrine ${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "Publisher" "Vitrine"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "NoRepair" 1

  ; Compute installed size for Add/Remove Programs
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine" \
    "EstimatedSize" $0

  ; Start Menu shortcut (all users get this)
  CreateDirectory "$SMPROGRAMS\Vitrine"
  CreateShortCut "$SMPROGRAMS\Vitrine\Vitrine.lnk" "$INSTDIR\Vitrine.exe"
  CreateShortCut "$SMPROGRAMS\Vitrine\Uninstall Vitrine.lnk" "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Desktop shortcut" SecDesktop
  CreateShortCut "$DESKTOP\Vitrine.lnk" "$INSTDIR\Vitrine.exe"
SectionEnd

Section "Start with Windows" SecAutostart
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" \
    "Vitrine" '"$INSTDIR\Vitrine.exe"'
SectionEnd

; ---------------------------------------------------------------------------
; Section descriptions
; ---------------------------------------------------------------------------
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Core Vitrine application files (required)."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} "Create a shortcut on the desktop."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecAutostart} "Automatically start Vitrine when you log in."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; ---------------------------------------------------------------------------
; Uninstaller
; ---------------------------------------------------------------------------
Section "Uninstall"
  ; Kill running instance
  nsExec::ExecToLog 'taskkill /F /IM Vitrine.exe'

  ; Remove files and directories
  RMDir /r "$INSTDIR"

  ; Start Menu
  RMDir /r "$SMPROGRAMS\Vitrine"

  ; Desktop shortcut
  Delete "$DESKTOP\Vitrine.lnk"

  ; Autostart registry key
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "Vitrine"

  ; App registry keys
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\Vitrine"
  DeleteRegKey HKCU "Software\Vitrine"

  ; Ask whether to remove user data
  MessageBox MB_YESNO|MB_ICONQUESTION \
    "Do you want to remove your Vitrine settings and themes?$\n$\n($APPDATA\Vitrine)" \
    IDYES removedata IDNO done

  removedata:
    RMDir /r "$APPDATA\Vitrine"

  done:
SectionEnd
