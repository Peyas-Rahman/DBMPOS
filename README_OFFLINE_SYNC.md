# DBM POS — Offline / Sync / Branch Server Quick Start

### Branch machine

1. Install SQL Server Express.
2. Set local DB connection in `DBM.POS.SyncService/appsettings.json` and API `appsettings.json`.
3. Set the same long random `Sync:ApiKey` in local API and central API.
4. Set CompanyId, BranchId and BranchServerId in SyncService config.
5. Set CentralUrl to the central HTTPS API.
6. Run `Publish-BranchServer.ps1` on Windows.
7. Run `Install-BranchServer.ps1` as Administrator.
8. Open `http://<branch-server-ip>:5000` on every POS terminal.

### Offline behavior

- Browser -> local API -> local SQL keeps working without internet.
- Every business change is written to `SyncQueueItems`.
- SyncService sends batches to `/api/sync/push`.
- Central returns accepted/conflict IDs.
- Successful rows become `Synced`.
- Failed rows remain retryable.
- Central changes are exposed through `/api/sync/changes`.
- Branch cursor is stored in `SyncCursors`.

### Troubleshooting

Check Windows Services:
- `DBM POS Local API`
- `DBM POS Sync Service`

Check local API:
- `http://localhost:5000`

Check Swagger during Development:
- `/swagger`

Check sync data in SQL:
- `SyncQueueItems`
- `SyncChanges`
- `SyncConflicts`
- `SyncCursors`
