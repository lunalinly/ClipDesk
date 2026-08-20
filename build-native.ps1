param(
  [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$distDir = Join-Path $projectRoot "dist"
$sourceFile = Join-Path $projectRoot "native\ClipDesk.cs"
$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$portableExe = Join-Path $distDir "ClipDesk-Portable-1.0.1-x64.exe"

if (-not (Test-Path -LiteralPath $compiler)) {
  throw "找不到 .NET Framework C# 編譯器：$compiler"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$compilerArgs = @(
  "/nologo",
  "/target:winexe",
  "/optimize+",
  "/define:PUBLIC_RELEASE,CUSTOM_CHROME",
  "/out:$portableExe",
  "/reference:System.Windows.Forms.dll",
  "/reference:System.Drawing.dll",
  "/reference:System.Web.Extensions.dll",
  $sourceFile
)

& $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "已建立免安裝版：$portableExe"

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
  Write-Warning "找不到 NSIS；免安裝版已完成，安裝版已略過。"
  exit 0
}

Push-Location (Join-Path $projectRoot "installer")
try {
  & $makeNsis "/V2" "ClipDesk.nsi"
  if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
  Pop-Location
}

Write-Host "已建立安裝版：$(Join-Path $distDir 'ClipDesk-Setup-1.0.1-x64.exe')"