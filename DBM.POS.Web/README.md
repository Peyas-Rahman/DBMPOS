# DBM POS Commercial v2

This package is based on the uploaded DBM.POS solution. The existing domain/entities/controllers were reused; duplicate Product/Sales controllers were not created.

## Functional frontend modules

- JWT login
- Live dashboard
- POS New Invoice
- Product search and barcode lookup
- Branch / warehouse selection
- Customer selection
- Cart / quantity / discount / paid / due
- Cash / Card / bKash / Nagad / Rocket / Credit payment selection
- Actual POST to `/api/sales`
- Product management
- Customer management
- Supplier management
- Sales invoice list/detail
- Purchase creation/list
- Inventory stock list
- Stock transfer create / dispatch / receive
- Master category / brand / unit management
- Organization/branch/warehouse view
- Expense entry/history
- Live sales / profit / stock / low-stock reports

## Run

### Backend

Open the solution in Visual Studio 2022/2026 with .NET 10 SDK installed.

Run `DBM.POS.API`.

The existing configuration is intended to expose the API on the configured local endpoint (the current project uses port 5000 in its development configuration).

### Frontend

```powershell
cd DBM.POS.Web
npm install
npm run dev
```

Open:

`http://localhost:5173`

Default seeded admin from the current project configuration:

- Username: `admin`
- Password: `12345`

Change the password before production use.

## API URL

Create `.env` from `.env.example` if needed:

```env
VITE_API_URL=http://localhost:5000/api
```

## Important

The menu contains the broader commercial ERP/POS navigation supplied for the project. The transaction-grade modules above are connected to the current backend controllers and database.

The existing backend also contains the foundations for:
- returns
- batches
- POS sessions
- payment methods
- ledgers
- sync journal
- branch server
- offline sync

Those existing APIs remain in the solution and can be connected to dedicated screens without replacing the current domain model.
