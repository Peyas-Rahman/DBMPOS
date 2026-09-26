# DBM POS Commercial Scope

## Included in this package

- Multi-company / multi-branch organization
- Branch server + POS device registration
- Product/category/brand/unit/variant/attribute masters
- Customers + customer ledger + due + payments
- Suppliers + supplier ledger + due + payments
- Purchases + purchase returns
- Sales + split payments + sales returns
- Stock balances + stock ledger + stock validation
- Stock transfers
- Batch master foundation
- POS sessions / cash control foundation
- Expenses + cash transactions
- Payment methods
- Reports / dashboard
- JWT authentication and roles/permissions foundation
- Browser POS UI served by the branch API
- PWA-style shell caching for the POS browser
- Offline-first branch database operation
- Automatic sync journal
- Retryable outbound sync queue
- Central change feed
- Branch sync cursor
- Idempotent transaction sync
- Master-data upsert sync
- Sync conflict storage
- Windows Sync Service
- Windows Branch Server deployment scripts
- LAN API on port 5000 for branch terminals

## Deployment model

`POS Browser -> Branch API -> Local SQL Server -> Sync Service -> Central API -> Central SQL`

Internet outage does not stop local POS operations. Only cloud synchronization is delayed until connectivity returns.

## Important production work

Before selling at scale, perform a real Windows/SQL Server build and acceptance test, generate formal EF migrations for the final model, configure HTTPS/reverse proxy for central API, rotate all secrets, configure SQL backups, test restore, printer/barcode hardware, and perform multi-branch concurrency/load testing.
