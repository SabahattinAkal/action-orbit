param(
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot 'ActionOrbit.slnx'
$projectPath = Join-Path $repoRoot 'src\ActionOrbit.App\ActionOrbit.App.csproj'
$testProjectPath = Join-Path $repoRoot 'tests\ActionOrbit.App.Tests\ActionOrbit.App.Tests.csproj'

function Assert-DotNetSuccess {
    param([string]$Step)
    if ($LASTEXITCODE -ne 0) {
        throw "$Step başarısız oldu (çıkış kodu: $LASTEXITCODE)."
    }
}

dotnet restore $solutionPath
Assert-DotNetSuccess 'Solution restore'
dotnet restore $projectPath --runtime $Runtime
Assert-DotNetSuccess 'Runtime restore'
dotnet build $solutionPath --configuration Release --no-restore
Assert-DotNetSuccess 'Release build'
dotnet test $testProjectPath --configuration Release --no-build --no-restore
Assert-DotNetSuccess 'Release tests'
dotnet publish $projectPath --configuration Release --no-restore "-p:PublishProfile=$Runtime"
Assert-DotNetSuccess 'Publish'

$publishPath = Join-Path $repoRoot "src\ActionOrbit.App\bin\publish\$Runtime"
Write-Host "Yayın paketi hazır: $publishPath"
