param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$Version = $Version.Trim().TrimStart('v', 'V')
if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must use the MAJOR.MINOR.PATCH format. Received: $Version"
}

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$stage = Join-Path $artifacts 'Raw-Accel-Reimagined-Windows-x64'
$package = Join-Path $artifacts 'Raw-Accel-Reimagined-Windows-x64.zip'
$checksum = "$package.sha256"

$msbuild = (Get-Command MSBuild.exe -ErrorAction SilentlyContinue).Source
if (-not $msbuild) {
    $frameworkMsBuild = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    if (Test-Path -LiteralPath $frameworkMsBuild) { $msbuild = $frameworkMsBuild }
}
if (-not $msbuild) { throw 'MSBuild.exe was not found.' }

& $msbuild (Join-Path $root 'modern-ui\RawAccelModern.csproj') /t:Rebuild /p:Configuration=Release /p:Platform=x64 /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'The main application build failed.' }
& $msbuild (Join-Path $root 'updater\RawAccelUpdater.csproj') /t:Rebuild /p:Configuration=Release /p:Platform=x64 /v:minimal
if ($LASTEXITCODE -ne 0) { throw 'The updater build failed.' }

$builtVersion = (Get-Item -LiteralPath (Join-Path $root 'modern-ui\bin\Release\RawAccelReimagined.exe')).VersionInfo.FileVersion
if (-not $builtVersion.StartsWith("$Version.")) {
    throw "The application file version ($builtVersion) does not match the release tag ($Version)."
}

if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package -Force }
if (Test-Path -LiteralPath $checksum) { Remove-Item -LiteralPath $checksum -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

$files = @(
    @{ Source = 'modern-ui\bin\Release\RawAccelReimagined.exe'; Target = 'RawAccelReimagined.exe' },
    @{ Source = 'modern-ui\RawAccelModern.exe.config'; Target = 'RawAccelReimagined.exe.config' },
    @{ Source = 'updater\bin\Release\RawAccelUpdater.exe'; Target = 'RawAccelUpdater.exe' },
    @{ Source = 'Newtonsoft.Json.dll'; Target = 'Newtonsoft.Json.dll' },
    @{ Source = 'wrapper.dll'; Target = 'wrapper.dll' },
    @{ Source = 'writer.exe'; Target = 'writer.exe' },
    @{ Source = 'rawaccel.exe'; Target = 'rawaccel.exe' },
    @{ Source = 'installer.exe'; Target = 'installer.exe' },
    @{ Source = 'uninstaller.exe'; Target = 'uninstaller.exe' },
    @{ Source = 'settings.example.json'; Target = 'settings.example.json' },
    @{ Source = 'LICENSE'; Target = 'LICENSE' },
    @{ Source = 'ReadMe.md'; Target = 'ReadMe.md' }
)

foreach ($file in $files) {
    $source = Join-Path $root $file.Source
    if (-not (Test-Path -LiteralPath $source)) { throw "Required release file is missing: $source" }
    $target = Join-Path $stage $file.Target
    $targetDirectory = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $targetDirectory)) { New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null }
    Copy-Item -LiteralPath $source -Destination $target -Force
}

Copy-Item -LiteralPath (Join-Path $root 'driver') -Destination (Join-Path $stage 'driver') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'doc') -Destination (Join-Path $stage 'doc') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $root 'themes') -Destination (Join-Path $stage 'themes') -Recurse -Force

Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $checksum -Value "$hash  Raw-Accel-Reimagined-Windows-x64.zip" -Encoding ASCII

Write-Host "Release package: $package"
Write-Host "SHA-256: $hash"
