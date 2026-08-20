param(
  [Parameter(Mandatory = $true)]
  [string[]]$Path,
  [switch]$RequireSigning
)

$ErrorActionPreference = "Stop"

function Find-SignTool {
  $command = Get-Command signtool.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -First 1
  if ($command) { return $command }
  $kitsRoot = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
  if (Test-Path -LiteralPath $kitsRoot) {
    $candidate = Get-ChildItem -LiteralPath $kitsRoot -Directory -ErrorAction SilentlyContinue |
      Sort-Object Name -Descending |
      ForEach-Object { Join-Path $_.FullName "x64\signtool.exe" } |
      Where-Object { Test-Path -LiteralPath $_ } |
      Select-Object -First 1
    if ($candidate) { return $candidate }
  }
  return $null
}

$pfxPath = $env:CLIPDESK_SIGNING_PFX_PATH
$pfxBase64 = $env:CLIPDESK_SIGNING_CERT_BASE64
$pfxPassword = $env:CLIPDESK_SIGNING_CERT_PASSWORD
$thumbprint = $env:CLIPDESK_SIGNING_CERT_THUMBPRINT
$storeLocation = $env:CLIPDESK_SIGNING_CERT_STORE
$timestampUrl = $env:CLIPDESK_TIMESTAMP_URL
if ([string]::IsNullOrWhiteSpace($timestampUrl)) { $timestampUrl = "http://timestamp.digicert.com" }

$hasPfx = -not [string]::IsNullOrWhiteSpace($pfxPath) -or -not [string]::IsNullOrWhiteSpace($pfxBase64)
$hasStoreCertificate = -not [string]::IsNullOrWhiteSpace($thumbprint)
if (-not $hasPfx -and -not $hasStoreCertificate) {
  if ($RequireSigning) { throw "要求簽章，但未設定 PFX 或 CLIPDESK_SIGNING_CERT_THUMBPRINT。" }
  Write-Warning "未設定程式碼簽章憑證；略過 Authenticode 簽章。"
  return
}

$signTool = Find-SignTool
if (-not $signTool) { throw "找不到 signtool.exe。請安裝 Windows SDK Signing Tools。" }

$temporaryPfx = $null
try {
  if (-not [string]::IsNullOrWhiteSpace($pfxBase64)) {
    $temporaryPfx = Join-Path ([IO.Path]::GetTempPath()) ("clipdesk-signing-" + [Guid]::NewGuid().ToString("N") + ".pfx")
    [IO.File]::WriteAllBytes($temporaryPfx, [Convert]::FromBase64String($pfxBase64))
    $pfxPath = $temporaryPfx
  }
  if ($hasPfx -and -not (Test-Path -LiteralPath $pfxPath)) { throw "找不到 PFX 憑證：$pfxPath" }

  foreach ($file in $Path) {
    $resolvedFile = (Resolve-Path -LiteralPath $file).Path
    $arguments = @("sign", "/fd", "SHA256", "/tr", $timestampUrl, "/td", "SHA256", "/d", "ClipDesk")
    if ($hasPfx) {
      $arguments += @("/f", $pfxPath)
      if (-not [string]::IsNullOrWhiteSpace($pfxPassword)) { $arguments += @("/p", $pfxPassword) }
    } else {
      $arguments += @("/sha1", ($thumbprint -replace "\s", ""))
      if ($storeLocation -ieq "LocalMachine") { $arguments += "/sm" }
    }
    $arguments += $resolvedFile
    & $signTool @arguments
    if ($LASTEXITCODE -ne 0) { throw "簽章失敗：$resolvedFile" }
    & $signTool verify /pa /v $resolvedFile
    if ($LASTEXITCODE -ne 0) { throw "簽章驗證失敗：$resolvedFile" }
    Write-Host "已簽章並驗證：$resolvedFile"
  }
} finally {
  if ($temporaryPfx -and (Test-Path -LiteralPath $temporaryPfx)) { [IO.File]::Delete($temporaryPfx) }
}
