[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$PackagePath,

    [Parameter(Mandatory)]
    [string]$HotfixPath,

    [Parameter(Mandatory)]
    [string]$ExpectedCommit,

    [Parameter(Mandatory)]
    [string]$Sha256SumsPath,

    [Parameter(Mandatory)]
    [string]$ReportPath
)

$ErrorActionPreference = "Stop"
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$hotfix = (Resolve-Path -LiteralPath $HotfixPath).Path
$sums = (Resolve-Path -LiteralPath $Sha256SumsPath).Path
$resolvedReport = [System.IO.Path]::GetFullPath($ReportPath)
$temporaryBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$workRoot = Join-Path $temporaryBase ("action-orbit-release-smoke-" + [guid]::NewGuid().ToString("N"))
$fullDirectory = Join-Path $workRoot "full"
$hotfixDirectory = Join-Path $workRoot "hotfix"
$cleanState = Join-Path $workRoot "clean-state"
$upgradeState = Join-Path $workRoot "upgrade-state"

function Assert-ArchiveEntriesSafe {
    param([string]$ArchivePath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            if ([string]::IsNullOrWhiteSpace($name) -or $name.EndsWith('/')) { continue }
            if ($name.StartsWith('/') -or $name -match '(^|/)\.\.(/|$)' -or [System.IO.Path]::IsPathRooted($name)) {
                throw "ZIP güvenli olmayan bir yol içeriyor."
            }
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-Checksum {
    param([string]$FilePath)

    $fileName = Split-Path $FilePath -Leaf
    $line = Get-Content -LiteralPath $sums | Where-Object { $_ -match "\s+$([regex]::Escape($fileName))$" } | Select-Object -First 1
    if (-not $line) { throw "SHA256SUMS içinde $fileName yok." }
    $expected = ($line -split '\s+')[0].ToLowerInvariant()
    $actual = (Get-FileHash -LiteralPath $FilePath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($expected -ne $actual) { throw "$fileName SHA-256 doğrulaması başarısız." }
}

function Assert-PackagePrivacy {
    param([string]$Directory)

    $forbiddenNames = @(
        'config.json',
        'shelves.json',
        'orbit-link.json',
        'orbit-link-queue.json',
        'actionorbit.log',
        'actionorbit.previous.log'
    )
    $files = Get-ChildItem -LiteralPath $Directory -Recurse -File
    foreach ($file in $files) {
        if ($file.Name -in $forbiddenNames -or
            $file.Extension -in '.pfx', '.snk', '.pem', '.key' -or
            $file.FullName -match '[\\/]shelf-cache[\\/]') {
            throw "Paket yerel durum veya imzalama materyali içeriyor: $($file.Name)"
        }
    }

    $privateValues = @($env:USERPROFILE, $env:USERNAME) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and $_.Length -ge 5 }
    $textExtensions = '.json', '.md', '.txt', '.config', '.xml'
    foreach ($file in $files | Where-Object { $_.Extension.ToLowerInvariant() -in $textExtensions }) {
        $text = [System.IO.File]::ReadAllText($file.FullName)
        foreach ($privateValue in $privateValues) {
            if ($text.IndexOf($privateValue, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                throw "Paket metni yerel kullanıcı veya profil bilgisi içeriyor: $($file.Name)"
            }
        }
    }
}

function Invoke-AppSmoke {
    param(
        [string]$Executable,
        [string]$StateDirectory,
        [string]$SmokeReport
    )

    New-Item -ItemType Directory -Path $StateDirectory -Force | Out-Null
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $Executable
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
    if ($SmokeReport.Contains('"') -or $StateDirectory.Contains('"')) {
        throw "Smoke yolları çift tırnak içeremez."
    }
    $startInfo.Arguments = "--release-smoke-report `"$SmokeReport`" --release-smoke-app-directory `"$StateDirectory`""
    $process = [System.Diagnostics.Process]::Start($startInfo)
    if (-not $process.WaitForExit(45000)) {
        $process.Kill($true)
        throw "Paket smoke testi 45 saniyede kapanmadı."
    }
    if ($process.ExitCode -ne 0) {
        throw "Paket smoke testi çıkış kodu $($process.ExitCode) ile başarısız oldu."
    }
    if (-not (Test-Path -LiteralPath $SmokeReport -PathType Leaf)) {
        throw "Uygulama smoke raporu üretmedi."
    }

    $result = Get-Content -LiteralPath $SmokeReport -Raw | ConvertFrom-Json
    if (-not $result.Succeeded) {
        throw "Uygulama XAML/tray smoke kontrolü başarısız: $($result.Error)"
    }
    $failedCheck = $result.Checks.PSObject.Properties | Where-Object { -not [bool]$_.Value }
    if ($failedCheck) {
        throw "Uygulama smoke kontrollerinden biri başarısız: $($failedCheck.Name -join ', ')"
    }
    return $result
}

function New-UpgradeFixture {
    param([string]$SourceState, [string]$DestinationState)

    Add-Type -AssemblyName System.Security
    New-Item -ItemType Directory -Path $DestinationState -Force | Out-Null
    Get-ChildItem -LiteralPath $SourceState -Force |
        Copy-Item -Destination $DestinationState -Recurse -Force
    $configPath = Join-Path $DestinationState 'config.json'
    $config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
    $config.configVersion = 9
    $preservedProfile = [pscustomobject]@{
        id = 'release_smoke_preserved'
        name = 'Release smoke preserved profile'
        mainRingName = 'Preserved ring'
        matches = @()
        actions = @()
        ringSets = @()
    }
    $config.profiles = @($config.profiles) + $preservedProfile
    $config | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $configPath -Encoding utf8

    $now = (Get-Date).ToUniversalTime().ToString('o')
    $shelves = [ordered]@{
        version = 1
        shelves = @(
            [ordered]@{
                id = 'release_smoke_shelf'
                name = 'Release smoke preserved shelf'
                isPinned = $true
                isShared = $false
                lastUsedUtc = $now
                items = @(
                    [ordered]@{
                        id = 'release_smoke_item'
                        kind = 'text'
                        displayName = 'Preserved item'
                        textContent = 'release-smoke-retention-marker'
                        createdUtc = $now
                    }
                )
            }
        )
    }
    $shelves | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $DestinationState 'shelves.json') -Encoding utf8

    $key = [byte[]]::new(32)
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $random.GetBytes($key) }
    finally { $random.Dispose() }
    $entropy = [System.Text.Encoding]::UTF8.GetBytes('ActionOrbit.OrbitLink.v1')
    $protectedKey = [System.Security.Cryptography.ProtectedData]::Protect(
        $key,
        $entropy,
        [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    $orbitLink = [ordered]@{
        version = 1
        deviceId = '11111111111111111111111111111111'
        deviceName = 'Release smoke device'
        enabled = $false
        listenPort = 48731
        peers = @(
            [ordered]@{
                id = '22222222222222222222222222222222'
                name = 'Preserved peer'
                host = '127.0.0.1'
                port = 48731
                protectedKey = [Convert]::ToBase64String($protectedKey)
                pairedUtc = $now
                lastSeenUtc = $now
            }
        )
    }
    $orbitLink | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $DestinationState 'orbit-link.json') -Encoding utf8
}

function Assert-UpgradePreserved {
    param([string]$StateDirectory)

    $config = Get-Content -LiteralPath (Join-Path $StateDirectory 'config.json') -Raw | ConvertFrom-Json
    if (-not ($config.profiles | Where-Object { $_.id -eq 'release_smoke_preserved' })) {
        throw "Yükseltme smoke testinde özel profil kayboldu."
    }
    $shelves = Get-Content -LiteralPath (Join-Path $StateDirectory 'shelves.json') -Raw | ConvertFrom-Json
    if (-not ($shelves.shelves | Where-Object { $_.id -eq 'release_smoke_shelf' })) {
        throw "Yükseltme smoke testinde raf kayboldu."
    }
    $orbitLink = Get-Content -LiteralPath (Join-Path $StateDirectory 'orbit-link.json') -Raw | ConvertFrom-Json
    if (-not ($orbitLink.peers | Where-Object { $_.id -eq '22222222222222222222222222222222' })) {
        throw "Yükseltme smoke testinde Orbit Link eşleşmesi kayboldu."
    }
}

try {
    New-Item -ItemType Directory -Path $fullDirectory, $hotfixDirectory -Force | Out-Null
    Assert-ArchiveEntriesSafe $package
    Assert-ArchiveEntriesSafe $hotfix
    Assert-Checksum $package
    Assert-Checksum $hotfix
    Expand-Archive -LiteralPath $package -DestinationPath $fullDirectory
    Expand-Archive -LiteralPath $hotfix -DestinationPath $hotfixDirectory
    Assert-PackagePrivacy $fullDirectory
    Assert-PackagePrivacy $hotfixDirectory

    $executable = Get-ChildItem -LiteralPath $fullDirectory -Recurse -Filter 'ActionOrbit.App.exe' | Select-Object -First 1
    $fullDll = Get-ChildItem -LiteralPath $fullDirectory -Recurse -Filter 'ActionOrbit.App.dll' | Select-Object -First 1
    $hotfixDll = Get-ChildItem -LiteralPath $hotfixDirectory -Recurse -Filter 'ActionOrbit.App.dll' | Select-Object -First 1
    if (-not $executable -or -not $fullDll -or -not $hotfixDll) {
        throw "Tam paket veya hotfix gerekli uygulama dosyalarını içermiyor."
    }

    $fullHash = (Get-FileHash -LiteralPath $fullDll.FullName -Algorithm SHA256).Hash
    $hotfixHash = (Get-FileHash -LiteralPath $hotfixDll.FullName -Algorithm SHA256).Hash
    if ($fullHash -ne $hotfixHash) { throw "Tam paket ve hotfix ActionOrbit.App.dll dosyaları aynı değil." }
    $productVersion = $fullDll.VersionInfo.ProductVersion
    if ($productVersion.IndexOf($ExpectedCommit, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Paket DLL'i beklenen committen üretilmemiş: $productVersion"
    }

    $cleanReportPath = Join-Path $workRoot 'clean-smoke.json'
    $cleanResult = Invoke-AppSmoke $executable.FullName $cleanState $cleanReportPath
    New-UpgradeFixture $cleanState $upgradeState
    $upgradeReportPath = Join-Path $workRoot 'upgrade-smoke.json'
    $upgradeResult = Invoke-AppSmoke $executable.FullName $upgradeState $upgradeReportPath
    Assert-UpgradePreserved $upgradeState

    $summary = [ordered]@{
        succeeded = $true
        expectedCommit = $ExpectedCommit
        productVersion = $productVersion
        packageSha256 = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash.ToLowerInvariant()
        hotfixSha256 = (Get-FileHash -LiteralPath $hotfix -Algorithm SHA256).Hash.ToLowerInvariant()
        cleanInstall = $cleanResult.Checks
        upgrade = $upgradeResult.Checks
        privacyScan = 'passed'
        statePreservation = 'passed'
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedReport) -Force | Out-Null
    $summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resolvedReport -Encoding utf8
    Write-Host "Release package smoke passed: $resolvedReport"
}
finally {
    $resolvedWorkRoot = [System.IO.Path]::GetFullPath($workRoot)
    if ($resolvedWorkRoot.StartsWith($temporaryBase, [StringComparison]::OrdinalIgnoreCase) -and
        $resolvedWorkRoot -ne $temporaryBase -and
        (Test-Path -LiteralPath $resolvedWorkRoot)) {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    }
}
