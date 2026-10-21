[CmdletBinding()]
param(
    [string]$DotNetPath = 'dotnet',
    [string]$IsccPath = '',
    [string]$Version = '2.0.0'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use major.minor.patch.' }
$buildId = (& git -C $repoRoot rev-parse --short HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine build commit.' }
$outputDir = Join-Path $repoRoot 'artifacts\setup'
# A fresh directory prevents stale files or local application state entering the installer.
$publishDir = Join-Path $repoRoot ('artifacts\setup-publish-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $outputDir, $publishDir -Force | Out-Null

if (-not $IsccPath) {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $IsccPath) {
        $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        if ($command) { $IsccPath = $command.Source }
    }
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw 'Install Inno Setup 6 or pass -IsccPath with the path to ISCC.exe.'
}

& $DotNetPath publish (Join-Path $repoRoot 'src\ActionOrbit.App\ActionOrbit.App.csproj') `
    --configuration Release --runtime win-x64 --self-contained true `
    '-p:PublishSingleFile=false' '-p:PublishTrimmed=false' '-p:DebugType=embedded' `
    '-p:RestoreLockedMode=true' --output $publishDir
if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed.' }

& $IsccPath "/DPublishDir=$publishDir" "/DOutputDir=$outputDir" "/DAppVersion=$Version" "/DBuildId=$buildId" `
    (Join-Path $repoRoot 'installer\ActionOrbit.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

$setupPath = Join-Path $outputDir "ActionOrbitPro-$Version-$buildId-Setup-win-x64.exe"
$hash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$setupPath.sha256", "$hash  $([IO.Path]::GetFileName($setupPath))`n")
Write-Output $setupPath
