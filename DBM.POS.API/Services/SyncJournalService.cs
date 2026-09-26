using System.Text.Json;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DBM.POS.API.Services;

public sealed class SyncJournalService
{
    private readonly POSDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public SyncJournalService(POSDbContext db) => _db = db;

    public async Task RecordAsync(Guid companyId, Guid branchId, string entityType, Guid transactionId, object payload, string operation = "Upsert", Guid? branchServerId = null)
    {
        if (transactionId == Guid.Empty) return;
        if (await _db.SyncQueueItems.AnyAsync(x => x.TransactionId == transactionId)) return;
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        _db.SyncQueueItems.Add(new SyncQueueItem
        {
            CompanyId = companyId,
            BranchId = branchId,
            BranchServerId = branchServerId,
            TransactionId = transactionId,
            EntityType = entityType,
            Operation = operation,
            Payload = json,
            Status = "Pending"
        });
    }

    public async Task RecordStockLedgersAsync(Guid companyId, Guid branchId, IEnumerable<StockLedger> ledgers)
    {
        foreach (var ledger in ledgers)
            await RecordAsync(companyId, branchId, "StockLedger", ledger.Id, ledger);
    }
}
