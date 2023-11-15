Push-Location
$buildLocation = Join-Path $PSScriptRoot "src/MKopa.SMSWorker"
Set-Location $buildLocation
dotnet cake
Pop-Location