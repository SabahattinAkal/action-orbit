param(
    [Parameter(Mandatory = $true)]
    [string]$FilePath,

    [Parameter(Mandatory = $true)]
    [string]$PfxPath,

    [string]$PasswordEnvironmentVariable = 'ACTION_ORBIT_CERTIFICATE_PASSWORD',

    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $FilePath -PathType Leaf)) {
    throw "İmzalanacak dosya bulunamadı: $FilePath"
}

if (-not (Test-Path -LiteralPath $PfxPath -PathType Leaf)) {
    throw "PFX sertifikası bulunamadı: $PfxPath"
}

$passwordText = [Environment]::GetEnvironmentVariable($PasswordEnvironmentVariable)
if ([string]::IsNullOrWhiteSpace($passwordText)) {
    throw "$PasswordEnvironmentVariable ortam değişkeni tanımlı değil."
}

$securePassword = ConvertTo-SecureString $passwordText -AsPlainText -Force
$importedCertificates = @()
$certificate = $null

try {
    $importedCertificates = @(Import-PfxCertificate `
        -FilePath $PfxPath `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -Password $securePassword `
        -Exportable:$false)

    $certificate = $importedCertificates |
        Where-Object {
            $_.HasPrivateKey -and
            @($_.EnhancedKeyUsageList | ForEach-Object { $_.ObjectId.Value }) -contains
                '1.3.6.1.5.5.7.3.3'
        } |
        Select-Object -First 1

    if ($null -eq $certificate) {
        throw 'PFX, özel anahtarlı bir kod imzalama sertifikası içermiyor.'
    }

    $signTool = Get-ChildItem `
        -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
        -Filter 'signtool.exe' `
        -File `
        -Recurse `
        -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($null -eq $signTool) {
        throw 'Windows SDK signtool.exe bulunamadı.'
    }

    & $signTool.FullName sign `
        /sha1 $certificate.Thumbprint `
        /fd SHA256 `
        /tr $TimestampUrl `
        /td SHA256 `
        $FilePath

    if ($LASTEXITCODE -ne 0) {
        throw "signtool başarısız oldu (çıkış kodu: $LASTEXITCODE)."
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $FilePath
    if ($signature.Status -ne 'Valid') {
        throw "İmza doğrulanamadı: $($signature.Status) - $($signature.StatusMessage)"
    }

    Write-Host "Kod imzası doğrulandı: $($certificate.Subject)"
}
finally {
    foreach ($importedCertificate in $importedCertificates) {
        if (-not [string]::IsNullOrWhiteSpace($importedCertificate.Thumbprint)) {
            Remove-Item `
                -LiteralPath "Cert:\CurrentUser\My\$($importedCertificate.Thumbprint)" `
                -Force `
                -ErrorAction SilentlyContinue
        }
    }

    $importedCertificates = @()
    $certificate = $null
    $passwordText = $null
    $securePassword = $null
}
