[CmdletBinding()]
param([Parameter(Mandatory)][string]$PublishDirectory)

$ErrorActionPreference = 'Stop'
$directory = (Resolve-Path -LiteralPath $PublishDirectory).Path
$required = 'ActionOrbit.App.exe', 'ActionOrbit.App.dll', 'ActionOrbit.App.deps.json', 'ActionOrbit.App.runtimeconfig.json'
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $directory $name) -PathType Leaf)) {
        throw 'Publish directory is missing a required application file.'
    }
}
$files = @(Get-ChildItem -LiteralPath $directory -Recurse -File -Force)
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($directory, $file.FullName)
    $allowed = ($file.Extension -eq '.dll') -or
        ($relative -in @('ActionOrbit.App.exe', 'createdump.exe', 'ActionOrbit.App.deps.json', 'ActionOrbit.App.runtimeconfig.json'))
    if (-not $allowed -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        # Never echo file names or content: a rejected file can itself contain private data.
        throw 'Publish privacy check rejected an unexpected file.'
    }
}
if (Get-ChildItem -LiteralPath $directory -Recurse -Directory -Force |
    Where-Object { $_.Name -in @('logs', 'shelf-cache', '.git') -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) }) {
    throw 'Publish privacy check rejected a state directory or symbolic link.'
}
Write-Host "Publish content check passed ($($files.Count) files)."
