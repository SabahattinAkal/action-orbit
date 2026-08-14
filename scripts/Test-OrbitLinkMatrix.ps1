[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("Home", "Office")]
    [string]$Role,

    [Parameter(Mandatory)]
    [string]$PeerAddress,

    [Parameter(Mandatory)]
    [ValidateSet("DirectBoth", "OfficeToHome", "HomeToOffice", "OfflineRecovery", "VpnRecovery")]
    [string]$Scenario,

    [Parameter(Mandatory)]
    [ValidateSet("Dark", "Light")]
    [string]$Theme,

    [string]$AppPath,
    [string]$OutputPath,
    [switch]$SkipAutomatedTests
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$port = 48731

function Get-PeerHost {
    param([string]$Value)

    $trimmed = $Value.Trim()
    if ($trimmed.StartsWith("[")) {
        $closing = $trimmed.IndexOf("]")
        if ($closing -le 1) { throw "IPv6 adresi geçersiz." }
        return $trimmed.Substring(1, $closing - 1)
    }

    $separator = $trimmed.LastIndexOf(":")
    if ($separator -gt 0 -and ($trimmed.ToCharArray() | Where-Object { $_ -eq ':' }).Count -eq 1) {
        $parsedPort = 0
        if ([int]::TryParse($trimmed.Substring($separator + 1), [ref]$parsedPort)) {
            $script:port = $parsedPort
            return $trimmed.Substring(0, $separator)
        }
    }
    return $trimmed
}

function Get-SafeAppVersion {
    param([string]$CandidatePath)

    if ($CandidatePath -and (Test-Path -LiteralPath $CandidatePath -PathType Leaf)) {
        return (Get-Item -LiteralPath $CandidatePath).VersionInfo.ProductVersion
    }

    $process = Get-Process -Name "ActionOrbit.App" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($process -and $process.Path) {
        return (Get-Item -LiteralPath $process.Path).VersionInfo.ProductVersion
    }
    return "not detected"
}

function Get-CommitSha {
    if (-not (Test-Path -LiteralPath (Join-Path $repoRoot ".git"))) { return "not available" }
    $sha = & git -C $repoRoot rev-parse HEAD 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $sha) { return "not available" }
    return $sha.Trim()
}

$peerHost = Get-PeerHost $PeerAddress
if ([string]::IsNullOrWhiteSpace($peerHost) -or $port -lt 1024 -or $port -gt 65535) {
    throw "PeerAddress IP:port biçiminde olmalı ve geçerli bir kullanıcı portu içermeli."
}

$operatingSystem = Get-CimInstance Win32_OperatingSystem
$connectionSucceeded = Test-NetConnection -ComputerName $peerHost -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
$appVersion = Get-SafeAppVersion $AppPath
$commitSha = Get-CommitSha
$tailscaleState = "not detected"
$tailscale = Get-Command tailscale -ErrorAction SilentlyContinue
if ($tailscale) {
    try {
        $status = (& $tailscale.Source status --json 2>$null | ConvertFrom-Json)
        $tailscaleState = if ($status.BackendState) { $status.BackendState } else { "available" }
    }
    catch {
        $tailscaleState = "available; status unavailable"
    }
}

$automatedTests = "skipped"
if (-not $SkipAutomatedTests -and (Test-Path -LiteralPath (Join-Path $repoRoot "ActionOrbit.slnx"))) {
    & dotnet test (Join-Path $repoRoot "tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj") `
        --configuration Release `
        --filter "FullyQualifiedName~OrbitLinkServiceTests" `
        --nologo
    $automatedTests = if ($LASTEXITCODE -eq 0) { "passed" } else { "failed" }
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $artifactDirectory = Join-Path $repoRoot "artifacts\orbit-link-matrix"
    $OutputPath = Join-Path $artifactDirectory ("{0}-{1}-{2:yyyyMMdd-HHmmss}.md" -f $Role, $Scenario, (Get-Date).ToUniversalTime())
}
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path (Split-Path -Parent $resolvedOutput) -Force | Out-Null

$connectionLabel = if ($connectionSucceeded) { "reachable" } else { "blocked or offline" }
$generatedUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
$report = @"
# Orbit Link real-device test run

- Generated: $generatedUtc
- Role: $Role
- Scenario: $Scenario
- Theme: $Theme
- Windows: $($operatingSystem.Caption), build $($operatingSystem.BuildNumber), $env:PROCESSOR_ARCHITECTURE
- Action Orbit: $appVersion
- Commit: $commitSha
- Outbound TCP ${port}: $connectionLabel
- Tailscale: $tailscaleState
- Orbit Link automated tests: $automatedTests

The report deliberately omits usernames, device names, IP addresses, file paths, pairing codes and Shelf content.

## Network

- [ ] Direct transfer follows the expected direction for this scenario.
- [ ] Reverse-channel transfer works when the opposite inbound direction is blocked.
- [ ] A queued item is delivered after the offline device returns.
- [ ] A queued item survives a VPN disconnect and reconnect.
- [ ] IPv4 and IPv4-mapped IPv6 peers behave the same.

## Content

- [ ] Text and URL arrive once.
- [ ] A Chrome-dragged image arrives and previews correctly.
- [ ] A small file arrives with matching size and hash.
- [ ] A 25 MB file is accepted.
- [ ] A file over 25 MB is rejected.
- [ ] A folder is rejected.
- [ ] Retrying the same transfer does not create a duplicate.

## Lifecycle and privacy

- [ ] Pairing code expires and cannot be reused.
- [ ] Tray Exit closes promptly during a transfer.
- [ ] A receiver with Shelf disabled rejects the item.
- [ ] Pairing remains after both apps restart.
- [ ] Logs and diagnostics contain no Shelf content or pairing code.

## Result

- [ ] Passed
- [ ] Failed; sanitized details are attached below.

Notes:
"@

[System.IO.File]::WriteAllText($resolvedOutput, $report, [System.Text.UTF8Encoding]::new($false))
Write-Host "Orbit Link matrix report: $resolvedOutput"
if ($automatedTests -eq "failed") { exit 1 }
