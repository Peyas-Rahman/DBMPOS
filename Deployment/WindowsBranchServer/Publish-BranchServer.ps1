param(
    [string]$Configuration = "Release",
    [string]$Output = ".\publish"
)
$ErrorActionPreference = 'Stop'

Write-Host "Publishing DBM POS Local API..." -ForegroundColor Cyan
dotnet publish .\DBM.POS.API\DBM.POS.API.csproj -c $Configuration -o "$Output\API" --self-contained false

Write-Host "Publishing DBM POS Sync Service..." -ForegroundColor Cyan
dotnet publish .\DBM.POS.SyncService\DBM.POS.SyncService.csproj -c $Configuration -o "$Output\SyncService" --self-contained false

Write-Host "Publish complete: $Output" -ForegroundColor Green
