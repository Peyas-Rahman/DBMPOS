param(
    [Parameter(Mandatory=$true)][string]$BranchId,
    [Parameter(Mandatory=$true)][string]$CompanyId,
    [Parameter(Mandatory=$true)][string]$BranchServerId,
    [Parameter(Mandatory=$true)][string]$CentralUrl,
    [Parameter(Mandatory=$true)][string]$SyncApiKey,
    [string]$SqlConnection = "Server=.\SQLEXPRESS;Database=DBMPOS;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    [string]$InstallRoot = "C:\Program Files\DBM SOFT\DBM POS"
)
$ErrorActionPreference = 'Stop'

if (-not (Test-Path ".\publish\API\DBM.POS.API.exe")) { throw "API publish not found. Run Publish-BranchServer.ps1 first." }
if (-not (Test-Path ".\publish\SyncService\DBM.POS.SyncService.exe")) { throw "Sync service publish not found. Run Publish-BranchServer.ps1 first." }

New-Item -ItemType Directory -Force -Path "$InstallRoot\API" | Out-Null
New-Item -ItemType Directory -Force -Path "$InstallRoot\SyncService" | Out-Null
Copy-Item ".\publish\API\*" "$InstallRoot\API" -Recurse -Force
Copy-Item ".\publish\SyncService\*" "$InstallRoot\SyncService" -Recurse -Force

$apiSettings = Join-Path $InstallRoot "API\appsettings.json"
$syncSettings = Join-Path $InstallRoot "SyncService\appsettings.json"

$api = Get-Content $apiSettings -Raw | ConvertFrom-Json
$api.ConnectionStrings.DefaultConnection = $SqlConnection
$api.Sync.ApiKey = $SyncApiKey
$api.Sync.BranchId = $BranchId
$api | ConvertTo-Json -Depth 20 | Set-Content $apiSettings -Encoding UTF8

$sync = Get-Content $syncSettings -Raw | ConvertFrom-Json
$sync.ConnectionStrings.DefaultConnection = $SqlConnection
$sync.Sync.CentralUrl = $CentralUrl.TrimEnd('/')
$sync.Sync.ApiKey = $SyncApiKey
$sync.Sync.CompanyId = $CompanyId
$sync.Sync.BranchId = $BranchId
$sync.Sync.BranchServerId = $BranchServerId
$sync | ConvertTo-Json -Depth 20 | Set-Content $syncSettings -Encoding UTF8

New-NetFirewallRule -DisplayName "DBM POS Branch API 5000" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow -Profile Domain,Private -ErrorAction SilentlyContinue | Out-Null

sc.exe stop "DBM POS Local API" | Out-Null
sc.exe delete "DBM POS Local API" | Out-Null
sc.exe create "DBM POS Local API" binPath= "`"$InstallRoot\API\DBM.POS.API.exe`"" start= auto DisplayName= "DBM POS Local API" | Out-Null
sc.exe failure "DBM POS Local API" reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

sc.exe stop "DBM POS Sync Service" | Out-Null
sc.exe delete "DBM POS Sync Service" | Out-Null
sc.exe create "DBM POS Sync Service" binPath= "`"$InstallRoot\SyncService\DBM.POS.SyncService.exe`"" start= auto DisplayName= "DBM POS Sync Service" | Out-Null
sc.exe failure "DBM POS Sync Service" reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

sc.exe start "DBM POS Local API" | Out-Null
sc.exe start "DBM POS Sync Service" | Out-Null

Write-Host "DBM POS Branch Server installed." -ForegroundColor Green
Write-Host "Local API: http://SERVER-IP:5000" -ForegroundColor Yellow
