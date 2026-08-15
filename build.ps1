param(
    [switch]$Run
)

$ErrorActionPreference =  "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root "ActionOrbit.slnx"
$exe = Join-Path $root "src\ActionOrbit.App\bin\Debug\net10.0-windows\ActionOrbit.App.exe"

Get-Process ActionOrbit.App -ErrorAction SilentlyContinue | Stop-Process -Force

dotnet build $solution

if ($Run) {
    Start-Process -FilePath $exe
}
