param(
  [switch]$SkipInstaller,
  [switch]$RequireSigning
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$distDir = Join-Path $projectRoot "dist"
$sourceFile = Join-Path $projectRoot "native\ClipDesk.cs"
$iconFile = Join-Path $projectRoot "assets\clipdesk.ico"
$signScript = Join-Path $projectRoot "sign-windows.ps1"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$version = "1.3.0"
$fullPortableExe = Join-Path $distDir "ClipDesk-Portable-$version-x64.exe"
$clipboardPortableExe = Join-Path $distDir "ClipDesk-Clipboard-Portable-$version-x64.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
  throw "找不到 .NET Framework C# 編譯器：$compiler"
}
if (-not (Test-Path -LiteralPath $iconFile)) {
  throw "找不到應用程式圖示：$iconFile"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

function Build-Portable([string]$outputPath, [string]$defines) {
  $compilerArgs = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/win32icon:$iconFile",
    "/define:$defines",
    "/out:$outputPath",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Web.Extensions.dll",
    $sourceFile
  )
  & $compiler @compilerArgs
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  Write-Host "已建立免安裝版：$outputPath"
}

Build-Portable $fullPortableExe "PUBLIC_RELEASE,CUSTOM_CHROME"
Build-Portable $clipboardPortableExe "PUBLIC_RELEASE,CUSTOM_CHROME,CLIPBOARD_ONLY"
& $signScript -Path @($fullPortableExe, $clipboardPortableExe) -RequireSigning:$RequireSigning

if ($SkipInstaller) { exit 0 }

$makeNsis = Get-Command makensis.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
if (-not $makeNsis) {
  $candidates = @(
    (Join-Path ${env:ProgramFiles(x86)} "NSIS\makensis.exe"),
    (Join-Path $env:ProgramFiles "NSIS\makensis.exe")
  )
  $makeNsis = $candidates | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
}

if (-not $makeNsis) {
  Write-Warning "找不到 NSIS；兩個免安裝版已完成，安裝版已略過。"
  exit 0
}

Push-Location (Join-Path $projectRoot "installer")
try {
  foreach ($script in @("ClipDesk.nsi", "ClipDesk.Clipboard.nsi")) {
    & $makeNsis "/V2" $script
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  }
} finally {
  Pop-Location
}

Write-Host "已建立安裝版：$(Join-Path $distDir "ClipDesk-Setup-$version-x64.exe")"
Write-Host "已建立安裝版：$(Join-Path $distDir "ClipDesk-Clipboard-Setup-$version-x64.exe")"
& $signScript -Path @(
  (Join-Path $distDir "ClipDesk-Setup-$version-x64.exe"),
  (Join-Path $distDir "ClipDesk-Clipboard-Setup-$version-x64.exe")
) -RequireSigning:$RequireSigning
