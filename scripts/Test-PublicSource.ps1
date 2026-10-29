[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
# Inspect the Git index: unrelated local drafts never enter this check or the commit.
$files = @(& git -C $repoRoot -c core.quotePath=false ls-files)
if ($LASTEXITCODE -ne 0) { throw 'Cannot read the Git index.' }
$textExtensions = '.cs', '.xaml', '.json', '.md', '.ps1', '.yml', '.yaml', '.xml', '.iss', '.csproj', '.pubxml'
$secretPattern = '(?i)(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,}|-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|AKIA[A-Z0-9]{16})'
$privateRoots = @($env:USERPROFILE) | Where-Object { $_ -and $_.Length -gt 5 }
foreach ($name in $files) {
    if ($name -match '(?i)(^|/)(config(?:\.lastgood|\.broken[^/]*)?\.json|shelves\.json|orbit-link(?:-queue)?\.json|\.env(?:\.(?!example$)[^/]+)?|[^/]+\.(?:pfx|p12|pem|key|snk|log|pdb))$' -or
        $name -match '(?i)(^|/)(shelf-cache|logs|artifacts|release)/') {
        throw 'Public source check rejected a local-state or credential file in the Git index.'
    }
    if ([IO.Path]::GetExtension($name) -notin $textExtensions) { continue }
    $content = @(& git -C $repoRoot show ":$name") -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect a staged file.' }
    if ($content -match $secretPattern) { throw 'Public source check found a credential pattern. Content is not printed.' }
    foreach ($root in $privateRoots) {
        foreach ($variant in @($root, $root.Replace('\', '/'), $root.Replace('\', '\\'))) {
            if ($content.Contains($variant, [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Public source check found a local user-profile path. Content is not printed.'
            }
        }
    }
}
Write-Output "Public source check passed ($($files.Count) indexed files; no file content logged)."
