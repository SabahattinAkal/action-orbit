[CmdletBinding()]
param([Parameter(Mandatory)][string]$DestinationDirectory)

$ErrorActionPreference = 'Stop'
$destination = [IO.Path]::GetFullPath($DestinationDirectory)
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$installer = Join-Path $destination 'innosetup-6.7.3.exe'
Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' -OutFile $installer
$expectedHash = '9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732'
if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedHash) {
    throw 'Compiler download SHA-256 mismatch.'
}
$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Pyrsys B\.V\.') {
    throw 'Compiler download signature verification failed.'
}
$compiler = Join-Path $destination 'compiler'
$process = Start-Process -FilePath $installer -ArgumentList @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART',
    '/CURRENTUSER', "/DIR=`"$compiler`"") -WindowStyle Hidden -PassThru
if (-not $process.WaitForExit(60000) -or $process.ExitCode -ne 0) { throw 'Compiler installation failed.' }
$iscc = Join-Path $compiler 'ISCC.exe'
if (-not (Test-Path -LiteralPath $iscc)) { throw 'Compiler executable missing.' }
Write-Output $iscc
