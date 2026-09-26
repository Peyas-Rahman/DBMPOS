$ErrorActionPreference = 'Continue'
sc.exe stop "DBM POS Sync Service"
sc.exe delete "DBM POS Sync Service"
sc.exe stop "DBM POS Local API"
sc.exe delete "DBM POS Local API"
Remove-NetFirewallRule -DisplayName "DBM POS Branch API 5000" -ErrorAction SilentlyContinue
Write-Host "DBM POS Branch Server services removed. Application files were not deleted." -ForegroundColor Yellow
