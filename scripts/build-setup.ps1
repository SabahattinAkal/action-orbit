[CmdletBinding()]
param(
    [string]$DotNetPath = 'dotnet',
    [string]$IsccPath = '',
    [string]$Version = '2.0.0',
    [string]$PublishDirectory = '',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version must use major.minor.patch.' }
$buildId = (& git -C $repoRoot rev-parse --short HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine build commit.' }
$outputDir = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $repoRoot 'artifacts\setup' }
# A fresh directory prevents stale files or local application state entering the installer.
$publishDir = if ($PublishDirectory) { (Resolve-Path -LiteralPath $PublishDirectory).Path } else {
    Join-Path $repoRoot ('artifacts\setup-publish-' + [guid]::NewGuid().ToString('N'))
}
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

if (-not $PublishDirectory) {
    & $DotNetPath publish (Join-Path $repoRoot 'src\ActionOrbit.App\ActionOrbit.App.csproj') `
    --configuration Release --runtime win-x64 --self-contained true `
    '-p:PublishSingleFile=false' '-p:PublishTrimmed=false' '-p:DebugType=embedded' `
    '-p:RestoreLockedMode=true' '-p:ContinuousIntegrationBuild=true' --output $publishDir
    if ($LASTEXITCODE -ne 0) { throw 'Self-contained publish failed.' }
}
& (Join-Path $PSScriptRoot 'Assert-PublishContent.ps1') -PublishDirectory $publishDir

$artworkDir = Join-Path $repoRoot ('artifacts\setup-artwork-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $artworkDir -Force | Out-Null
Add-Type -AssemblyName System.Drawing
$logo = [Drawing.Image]::FromFile((Join-Path $repoRoot 'src\ActionOrbit.App\Assets\Brand\ActionOrbitLogo.png'))
try {
    foreach ($size in @(@(164, 314, 'wizard.bmp'), @(55, 55, 'wizard-small.bmp'))) {
        $bitmap = [Drawing.Bitmap]::new([int]$size[0], [int]$size[1])
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([Drawing.Color]::FromArgb(15, 17, 22))
            $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $logoSize = if ($size[0] -eq 164) { 110 } else { 47 }
            $top = if ($size[0] -eq 164) { 68 } else { 4 }
            $graphics.DrawImage($logo, [int](($size[0] - $logoSize) / 2), $top, $logoSize, $logoSize)
            $bitmap.Save((Join-Path $artworkDir $size[2]), [Drawing.Imaging.ImageFormat]::Bmp)
        }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }
}
finally { $logo.Dispose() }

& $IsccPath "/DPublishDir=$publishDir" "/DOutputDir=$outputDir" "/DArtworkDir=$artworkDir" "/DAppVersion=$Version" "/DBuildId=$buildId" `
    (Join-Path $repoRoot 'installer\ActionOrbit.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }

$setupPath = Join-Path $outputDir "ActionOrbitPro-$Version-$buildId-Setup-win-x64.exe"
$hash = (Get-FileHash -LiteralPath $setupPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$setupPath.sha256", "$hash  $([IO.Path]::GetFileName($setupPath))`n")
Write-Output $setupPath
