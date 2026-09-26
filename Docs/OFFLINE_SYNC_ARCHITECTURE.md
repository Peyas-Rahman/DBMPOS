# DBM POS — Offline + Sync + Branch Server

## Runtime architecture

POS terminals use the branch LAN API. They never connect directly to SQL Server.

`POS Browser -> Branch API :5000 -> Local SQL Server -> Sync Service -> Central API -> Central SQL`

Internet failure does not stop local sales, purchases, returns, stock, cash sessions or customer transactions.

## Sync guarantees

- Every local transaction has a GUID.
- Local transactions are first committed to the local database.
- `SyncQueueItems` stores the outbound payload.
- Sync retries failed records.
- Central `/api/sync/push` is idempotent for append-only transaction records.
- `SyncChanges` is the central change feed.
- Each branch stores a `SyncCursor` and pulls only newer changes.
- Master data uses upsert semantics.
- Conflicts are stored in `SyncConflicts` instead of being silently discarded.

## Deployment

1. Install SQL Server Express on the branch server.
2. Publish API and SyncService with `Publish-BranchServer.ps1`.
3. Run `Install-BranchServer.ps1` as Administrator.
4. Open `http://<server-ip>:5000` from POS terminals.
5. Configure each terminal with the branch server URL.

## Important

The API project in this repository is the same application used locally and centrally. A central deployment should be placed behind HTTPS/reverse proxy and should use a long random `Sync:ApiKey`.
