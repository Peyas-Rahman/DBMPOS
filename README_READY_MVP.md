# DBM POS – Ready MVP

This package extends the existing DBM.POS .NET 10 / SQL Server project.

## Included
- JWT authentication and role/permission claims
- Multi-company / branch / warehouse foundation
- Product, category, brand, unit
- Product variants and configurable attributes
- Customer / supplier
- Purchase and stock-in
- Sales / POS and stock-out
- Sales return / purchase return
- Stock balance + stock ledger
- Stock adjustment
- Stock transfer: Requested -> Approved/Requested -> In Transit -> Received
- Dashboard summary
- User and role management
- Platform company onboarding
- Browser UI at `/`
- Swagger at `/swagger`

## Existing database
The project keeps the existing EF migration and automatically creates the new business tables at startup if they do not exist. This is intentional for the ready-MVP package. After opening the solution, you can later create a normal EF migration with:

```powershell
Add-Migration AddBusinessModules -Project DBM.POS.Infrastructure -StartupProject DBM.POS.API
Update-Database -Project DBM.POS.Infrastructure -StartupProject DBM.POS.API
```

## Run
1. Open `DBM.POS.slnx` in Visual Studio.
2. Confirm SQL Server connection in `DBM.POS.API/appsettings.json`.
3. Build the solution.
4. Run `DBM.POS.API`.
5. Open the shown HTTPS URL.
6. Login with the configured InitialAdmin credentials.
7. Swagger: `/swagger`
8. Web UI: `/`

## Important
The current initial admin password is configured in appsettings. Change it before production use.

## Architecture
Browser/POS -> ASP.NET Core API -> Local SQL Server

The same API is intended to be reused by future mobile/cloud clients.

## Commercial next phase
The current package is a functional backend/MVP foundation. Before selling to production customers, add:
- Offline sync engine
- Cloud central API/database
- Subscription/license enforcement
- Full accounting ledger
- Batch/expiry transaction detail
- Barcode printing
- Receipt thermal printing
- Audit log
- Backup/restore automation
- Comprehensive return/due settlement workflows
- Production-grade frontend and permissions at endpoint level
