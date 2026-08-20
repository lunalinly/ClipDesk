Unicode true
Name "ClipDesk 剪貼簿版"
OutFile "..\dist\ClipDesk-Clipboard-Setup-1.3.0-x64.exe"
Icon "..\assets\clipdesk.ico"
UninstallIcon "..\assets\clipdesk.ico"
InstallDir "$LOCALAPPDATA\Programs\ClipDesk Clipboard"
InstallDirRegKey HKCU "Software\ClipDeskClipboard" "InstallDir"
RequestExecutionLevel user
ShowInstDetails show
ShowUninstDetails show

Page directory
Page instfiles
UninstPage uninstConfirm
UninstPage instfiles

Section "ClipDesk 剪貼簿版" SEC_MAIN
  SetOutPath "$INSTDIR"
  File /oname=ClipDesk-Clipboard.exe "..\dist\ClipDesk-Clipboard-Portable-1.3.0-x64.exe"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\ClipDeskClipboard" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDeskClipboard" "DisplayName" "ClipDesk 剪貼簿版"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDeskClipboard" "DisplayVersion" "1.3.0"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDeskClipboard" "Publisher" "ClipDesk"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDeskClipboard" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  CreateDirectory "$SMPROGRAMS\ClipDesk 剪貼簿版"
  CreateShortcut "$SMPROGRAMS\ClipDesk 剪貼簿版\ClipDesk 剪貼簿版.lnk" "$INSTDIR\ClipDesk-Clipboard.exe"
  CreateShortcut "$SMPROGRAMS\ClipDesk 剪貼簿版\解除安裝 ClipDesk 剪貼簿版.lnk" "$INSTDIR\Uninstall.exe"
  CreateShortcut "$DESKTOP\ClipDesk 剪貼簿版.lnk" "$INSTDIR\ClipDesk-Clipboard.exe"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\ClipDesk 剪貼簿版.lnk"
  Delete "$SMPROGRAMS\ClipDesk 剪貼簿版\ClipDesk 剪貼簿版.lnk"
  Delete "$SMPROGRAMS\ClipDesk 剪貼簿版\解除安裝 ClipDesk 剪貼簿版.lnk"
  RMDir "$SMPROGRAMS\ClipDesk 剪貼簿版"
  Delete "$INSTDIR\ClipDesk-Clipboard.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\ClipDeskClipboard"
  DeleteRegKey HKCU "Software\ClipDeskClipboard"
SectionEnd
