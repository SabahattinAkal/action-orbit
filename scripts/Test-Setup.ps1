[CmdletBinding()]
param([Parameter(Mandatory)][string]$SetupPath)

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
        Invoke-HiddenProcess $setup @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', '/NOICONS',
            '/NOCLOSEAPPLICATIONS', '/NORESTARTAPPLICATIONS', "/DIR=`"$installDir`"", "/LOG=`"$testRoot\install-$pass.log`"")
        if (-not (Test-Path -LiteralPath "$installDir\ActionOrbit.App.exe")) { throw 'Installed executable missing.' }
        if (-not (Test-Path $uninstallKey)) { throw 'Windows uninstall registration missing.' }
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
    if ($report.Checks.PSObject.Properties | Where-Object { -not [bool]$_.Value }) { throw 'An app smoke check failed.' }
}
finally {
    $uninstaller = Join-Path $installDir 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        Invoke-HiddenProcess $uninstaller @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/LOG=`"$testRoot\uninstall.log`"")
    }
}

if (Test-Path -LiteralPath "$installDir\ActionOrbit.App.exe") { throw 'Uninstall left the executable behind.' }
if (Test-Path $uninstallKey) { throw 'Uninstall registration was not removed.' }
if (-not (Test-Path -LiteralPath "$stateDir\config.json")) { throw 'Uninstall removed data outside its installation directory.' }
Write-Output "PASS: install, in-place update, installed app smoke, package privacy, uninstall. Logs: $testRoot"
