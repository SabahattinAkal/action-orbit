[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SetupPath,
    [string]$ReportPath = '',
    [string]$ExpectedCommit = ''
)

$ErrorActionPreference = 'Stop'
$setup = (Resolve-Path -LiteralPath $SetupPath).Path
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{5F828BC8-56F7-4538-9B7B-05818EB27780}_is1'
if (Test-Path $uninstallKey) {
    throw 'An installed copy already exists. Run this test in a separate Windows account.'
}
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('action-orbit-setup-test-' + [guid]::NewGuid().ToString('N'))
$installDir = Join-Path $testRoot 'installed'
$stateDir = Join-Path $testRoot 'state'
$reportPath = Join-Path $testRoot 'app-smoke.json'
New-Item -ItemType Directory -Path $testRoot, $stateDir -Force | Out-Null
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$preferenceKey = 'HKCU:\Software\ActionOrbitPro\Setup'
$originalRun = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro', $null)
$originalPreference = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\ActionOrbitPro\Setup', 'RunAtStartup', $null)

function Invoke-HiddenProcess([string]$FilePath, [string[]]$Arguments) {
    $process = Start-Process -FilePath $FilePath -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(60000)) {
        throw 'Setup test process did not exit within 60 seconds.'
    }
    if ($process.ExitCode -ne 0) { throw "Test process exited with code $($process.ExitCode)." }
}

try {
    # Install twice to exercise an in-place update with the same AppId.
    foreach ($pass in 1..2) {
        $tasks = if ($pass -eq 1) { '/TASKS=startup' } else { '/TASKS=!startup' }
        Invoke-HiddenProcess $setup @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOICONS',
            '/NOCLOSEAPPLICATIONS', '/NORESTARTAPPLICATIONS', $tasks, "/DIR=`"$installDir`"", "/LOG=`"$testRoot\install-$pass.log`"")
        if (-not (Test-Path -LiteralPath "$installDir\ActionOrbit.App.exe")) { throw 'Installed executable missing.' }
        if (-not (Test-Path $uninstallKey)) { throw 'Windows uninstall registration missing.' }
        $choice = Get-ItemPropertyValue -LiteralPath $preferenceKey -Name RunAtStartup
        if ($choice -ne [int]($pass -eq 1)) { throw 'Setup startup preference does not match the selected task.' }
        $run = [Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run', 'ActionOrbitPro', $null)
        if ($pass -eq 1 -and $run -ne ('"' + $installDir + '\ActionOrbit.App.exe" --background')) { throw 'Startup command missing or incorrect.' }
        if ($pass -eq 2 -and $null -ne $run) { throw 'Deselected startup task left the Run entry behind.' }
    }

    $forbidden = Get-ChildItem -LiteralPath $installDir -Recurse -File | Where-Object {
        $_.Name -in @('config.json', 'shelves.json', 'orbit-link.json', 'orbit-link-queue.json') -or
        $_.Extension -in @('.pfx', '.pem', '.key', '.pdb')
    }
    if ($forbidden) { throw 'Installer contains local state or private build files.' }

    Invoke-HiddenProcess "$installDir\ActionOrbit.App.exe" @('--release-smoke-report', "`"$reportPath`"",
        '--release-smoke-app-directory', "`"$stateDir`"")
    $report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if (-not $report.Succeeded) { throw "Installed app smoke failed: $($report.Error)" }
    if ($ExpectedCommit -and -not $report.ProductVersion.Contains($ExpectedCommit, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Installed application does not match the expected commit.'
    }
    if ($report.Checks.PSObject.Properties | Where-Object { -not [bool]$_.Value }) { throw 'An app smoke check failed.' }
}
finally {
    $uninstaller = Join-Path $installDir 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        Invoke-HiddenProcess $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$testRoot\uninstall.log`"")
    }
    if ($null -ne $originalRun) {
        New-ItemProperty -LiteralPath $runKey -Name ActionOrbitPro -Value $originalRun -PropertyType String -Force | Out-Null
    }
    if ($null -ne $originalPreference) {
        New-Item -Path $preferenceKey -Force | Out-Null
        New-ItemProperty -LiteralPath $preferenceKey -Name RunAtStartup -Value $originalPreference -PropertyType DWord -Force | Out-Null
    }
}

if (Test-Path -LiteralPath "$installDir\ActionOrbit.App.exe") { throw 'Uninstall left the executable behind.' }
if (Test-Path $uninstallKey) { throw 'Uninstall registration was not removed.' }
if (-not (Test-Path -LiteralPath "$stateDir\config.json")) { throw 'Uninstall removed data outside its installation directory.' }
$summary = [ordered]@{
    succeeded = $true
    productVersion = $report.ProductVersion
    setupSha256 = (Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash.ToLowerInvariant()
    install = $true
    inPlaceUpdate = $true
    startupEnabled = $true
    startupDisabled = $true
    appSmoke = $report.Checks
    packagePrivacy = $true
    uninstall = $true
    dataPreserved = $true
}
if ($ReportPath) {
    $destination = [IO.Path]::GetFullPath($ReportPath)
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $destination -Encoding utf8
}
Write-Output 'PASS: install, update, startup choices, app smoke, privacy, uninstall, data preservation.'
