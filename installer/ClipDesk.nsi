Unicode true
Name "ClipDesk"
OutFile "..\dist\ClipDesk-Setup-1.0.1-x64.exe"
InstallDir "$LOCALAPPDATA\Programs\ClipDesk"
InstallDirRegKey HKCU "Software\ClipDesk" "InstallDir"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "ClipDesk" SEC_MAIN
  SetOutPath "$INSTDIR"
  File /oname=ClipDesk.exe "..\dist\ClipDesk-Portable-1.0.1-x64.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\ClipDesk" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDesk" "DisplayName" "ClipDesk"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDesk" "DisplayVersion" "1.0.1"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDesk" "Publisher" "ClipDesk"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDesk" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  CreateDirectory "$SMPROGRAMS\ClipDesk"
  CreateShortcut "$SMPROGRAMS\ClipDesk\ClipDesk.lnk" "$INSTDIR\ClipDesk.exe"
  CreateShortcut "$SMPROGRAMS\ClipDesk\解除安裝 ClipDesk.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\ClipDesk.lnk" "$INSTDIR\ClipDesk.exe"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\ClipDesk.lnk"
  Delete "$SMPROGRAMS\ClipDesk\ClipDesk.lnk"
  Delete "$SMPROGRAMS\ClipDesk\解除安裝 ClipDesk.lnk"
  RMDir "$SMPROGRAMS\ClipDesk"
  Delete "$INSTDIR\ClipDesk.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDesk"
  DeleteRegKey HKCU "Software\ClipDesk"
SectionEnd
