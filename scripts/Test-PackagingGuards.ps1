[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Join-Path ([IO.Path]::GetTempPath()) ('action-orbit-package-guard-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root -Force | Out-Null
$required = 'ActionOrbit.App.exe', 'ActionOrbit.App.dll', 'ActionOrbit.App.deps.json', 'ActionOrbit.App.runtimeconfig.json'
foreach ($name in $required) { [IO.File]::WriteAllText((Join-Path $root $name), '{}') }
try {
    & "$PSScriptRoot\Assert-PublishContent.ps1" -PublishDirectory $root
    foreach ($name in @('config.json', 'shelves.json', 'orbit-link.json', '.env', 'certificate.pfx', 'ActionOrbit.App.pdb', 'notes.txt', 'unexpected.exe')) {
        $fixture = Join-Path $root $name
        [IO.File]::WriteAllText($fixture, 'synthetic-test-only')
        $rejected = $false
        try { & "$PSScriptRoot\Assert-PublishContent.ps1" -PublishDirectory $root }
        catch { $rejected = $true }
        finally { Remove-Item -LiteralPath $fixture -Force }
        if (-not $rejected) { throw 'Packaging guard accepted a forbidden fixture.' }
    }
    Write-Output 'PASS: clean publish accepted; eight forbidden file fixtures rejected.'
}
finally {
    foreach ($name in $required) { Remove-Item -LiteralPath (Join-Path $root $name) -Force }
    Remove-Item -LiteralPath $root
}
