using System.Text.Json;
using DBM.POS.API.DTOs.Sync;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DBM.POS.API.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController : ControllerBase
{
    private readonly POSDbContext _db;
    private readonly IConfiguration _config;
    private static readonly SemaphoreSlim SequenceLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SyncController(POSDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private bool IsValidKey()
    {
        var expected = _config["Sync:ApiKey"];
        if (string.IsNullOrWhiteSpace(expected)) return false;
        return Request.Headers.TryGetValue("X-Sync-Key", out var supplied) && supplied == expected;
    }

    [HttpGet("pending")]
    [AllowAnonymous]
    public async Task<IActionResult> Pending([FromQuery] Guid companyId, [FromQuery] Guid branchId, [FromQuery] int take = 200)
    {
        if (!IsValidKey()) return Unauthorized();
        take = Math.Clamp(take, 1, 1000);
        var items = await _db.SyncQueueItems.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.BranchId == branchId && (x.Status == "Pending" || x.Status == "Failed"))
            .OrderBy(x => x.CreatedAt).Take(take)
            .Select(x => new SyncEnvelope(x.TransactionId, x.CompanyId, x.BranchId, x.BranchServerId, x.EntityType, x.Operation, x.CreatedAt, x.Payload))
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("push")]
    [AllowAnonymous]
    public async Task<IActionResult> Push(SyncPushRequest request)
    {
        if (!IsValidKey()) return Unauthorized();
        var accepted = new List<Guid>();
        var conflicts = new List<object>();

        foreach (var item in request.Items ?? [])
        {
            try
            {
                if (await AlreadyAppliedAsync(item))
                {
                    accepted.Add(item.TransactionId);
                    continue;
                }

                await ApplyAsync(item);
                accepted.Add(item.TransactionId);
                await AddChangeAsync(item);
            }
            catch (Exception ex)
            {
                conflicts.Add(new { item.TransactionId, item.EntityType, message = ex.Message });
                _db.SyncConflicts.Add(new SyncConflict
                {
                    CompanyId = item.CompanyId,
                    BranchId = item.BranchId,
                    TransactionId = item.TransactionId,
                    EntityType = item.EntityType,
                    Payload = item.Payload,
                    Reason = ex.Message
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { accepted, conflicts });
    }

    [HttpPost("ack")]
    [AllowAnonymous]
    public async Task<IActionResult> Ack(SyncAckRequest request)
    {
        if (!IsValidKey()) return Unauthorized();
        var ids = request.TransactionIds ?? [];
        var rows = await _db.SyncQueueItems.Where(x => ids.Contains(x.TransactionId)).ToListAsync();
        foreach (var row in rows)
        {
            row.Status = "Synced";
            row.SyncedAt = DateTime.UtcNow;
            row.LastAttemptAt = DateTime.UtcNow;
            row.LastError = null;
        }
        await _db.SaveChangesAsync();
        return Ok(new { synced = rows.Count });
    }

    [HttpGet("changes")]
    [AllowAnonymous]
    public async Task<IActionResult> Changes([FromQuery] Guid companyId, [FromQuery] Guid branchId, [FromQuery] long afterSequence = 0, [FromQuery] int take = 200)
    {
        if (!IsValidKey()) return Unauthorized();
        take = Math.Clamp(take, 1, 1000);
        var items = await _db.SyncChanges.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.Sequence > afterSequence && x.BranchId != branchId)
            .OrderBy(x => x.Sequence).Take(take)
            .Select(x => new { x.Sequence, x.TransactionId, x.CompanyId, x.BranchId, x.EntityType, x.Operation, x.Payload, x.CreatedAt })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost("mark-attempt")]
    [AllowAnonymous]
    public async Task<IActionResult> MarkAttempt([FromBody] Guid[] transactionIds)
    {
        if (!IsValidKey()) return Unauthorized();
        var rows = await _db.SyncQueueItems.Where(x => transactionIds.Contains(x.TransactionId)).ToListAsync();
        foreach (var row in rows) { row.RetryCount++; row.LastAttemptAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task<bool> AlreadyAppliedAsync(SyncEnvelope item)
    {
        return item.EntityType switch
        {
            "Sale" => await _db.Sales.AnyAsync(x => x.Id == item.TransactionId),
            "Purchase" => await _db.Purchases.AnyAsync(x => x.Id == item.TransactionId),
            "StockLedger" => await _db.StockLedgers.AnyAsync(x => x.Id == item.TransactionId),
            "CustomerLedger" => await _db.CustomerLedgers.AnyAsync(x => x.Id == item.TransactionId),
            "SupplierLedger" => await _db.SupplierLedgers.AnyAsync(x => x.Id == item.TransactionId),
            "Expense" => await _db.Expenses.AnyAsync(x => x.Id == item.TransactionId),
            "CashTransaction" => await _db.CashTransactions.AnyAsync(x => x.Id == item.TransactionId),
            "Payment" => await _db.Payments.AnyAsync(x => x.Id == item.TransactionId),
            "Customer" or "Supplier" or "Product" or "ProductVariant" or "Category" or "Brand" or "Unit" => false,
            _ => false
        };
    }

    private async Task ApplyAsync(SyncEnvelope item)
    {
        _db.SyncJournalDisabled = true;
        try
        {
        switch (item.EntityType)
        {
            case "Sale":
                var sale = JsonSerializer.Deserialize<Sale>(item.Payload, JsonOptions) ?? throw new InvalidOperationException("Invalid sale payload.");
                sale.Customer = null;
                _db.Sales.Add(sale);
                break;
            case "Purchase":
                var purchase = JsonSerializer.Deserialize<Purchase>(item.Payload, JsonOptions) ?? throw new InvalidOperationException("Invalid purchase payload.");
                purchase.Supplier = null;
                _db.Purchases.Add(purchase);
                break;
            case "StockLedger":
                await ApplyStockLedgerAsync(JsonSerializer.Deserialize<StockLedger>(item.Payload, JsonOptions) ?? throw new InvalidOperationException("Invalid stock payload."));
                break;
            case "CustomerLedger": _db.CustomerLedgers.Add(JsonSerializer.Deserialize<CustomerLedger>(item.Payload, JsonOptions)!); break;
            case "SupplierLedger": _db.SupplierLedgers.Add(JsonSerializer.Deserialize<SupplierLedger>(item.Payload, JsonOptions)!); break;
            case "Expense": _db.Expenses.Add(JsonSerializer.Deserialize<Expense>(item.Payload, JsonOptions)!); break;
            case "CashTransaction": _db.CashTransactions.Add(JsonSerializer.Deserialize<CashTransaction>(item.Payload, JsonOptions)!); break;
            case "Payment": _db.Payments.Add(JsonSerializer.Deserialize<Payment>(item.Payload, JsonOptions)!); break;
            case "Customer": await UpsertAsync(_db.Customers, JsonSerializer.Deserialize<Customer>(item.Payload, JsonOptions)!); break;
            case "Supplier": await UpsertAsync(_db.Suppliers, JsonSerializer.Deserialize<Supplier>(item.Payload, JsonOptions)!); break;
            case "Product": await UpsertAsync(_db.Products, JsonSerializer.Deserialize<Product>(item.Payload, JsonOptions)!); break;
            case "ProductVariant": await UpsertAsync(_db.ProductVariants, JsonSerializer.Deserialize<ProductVariant>(item.Payload, JsonOptions)!); break;
            case "Category": await UpsertAsync(_db.Categories, JsonSerializer.Deserialize<Category>(item.Payload, JsonOptions)!); break;
            case "Brand": await UpsertAsync(_db.Brands, JsonSerializer.Deserialize<Brand>(item.Payload, JsonOptions)!); break;
            case "Unit": await UpsertAsync(_db.Units, JsonSerializer.Deserialize<Unit>(item.Payload, JsonOptions)!); break;
            default: throw new InvalidOperationException($"Unsupported sync entity: {item.EntityType}");
        }
        await _db.SaveChangesAsync();
        }
        finally { _db.SyncJournalDisabled = false; }
    }

    private async Task UpsertAsync<TEntity>(DbSet<TEntity> set, TEntity entity) where TEntity : class
    {
        var id = (Guid)(typeof(TEntity).GetProperty("Id")?.GetValue(entity) ?? Guid.Empty);
        var existing = await set.FindAsync(id);
        if (existing == null) set.Add(entity);
        else _db.Entry(existing).CurrentValues.SetValues(entity);
    }

    private async Task ApplyStockLedgerAsync(StockLedger source)
    {
        if (await _db.StockLedgers.AnyAsync(x => x.Id == source.Id)) return;
        var balance = await _db.StockBalances.FirstOrDefaultAsync(x => x.CompanyId == source.CompanyId && x.WarehouseId == source.WarehouseId && x.ProductId == source.ProductId && x.VariantId == source.VariantId);
        var delta = source.QuantityIn - source.QuantityOut;
        if (balance == null)
        {
            balance = new StockBalance { CompanyId = source.CompanyId, WarehouseId = source.WarehouseId, ProductId = source.ProductId, VariantId = source.VariantId, Quantity = 0, AverageCost = source.UnitCost };
            _db.StockBalances.Add(balance);
        }
        var old = balance.Quantity;
        var next = old + delta;
        if (delta > 0 && next > 0) balance.AverageCost = ((old * balance.AverageCost) + (delta * source.UnitCost)) / next;
        balance.Quantity = next;
        balance.UpdatedAt = DateTime.UtcNow;
        _db.StockLedgers.Add(source);
        await _db.SaveChangesAsync();
    }

    private async Task AddChangeAsync(SyncEnvelope item)
    {
        await SequenceLock.WaitAsync();
        try
        {
            var sequence = await _db.SyncChanges.Select(x => (long?)x.Sequence).MaxAsync() ?? 0;
            _db.SyncChanges.Add(new SyncChange
            {
                Sequence = sequence + 1,
                CompanyId = item.CompanyId,
                BranchId = item.BranchId,
                TransactionId = item.TransactionId,
                EntityType = item.EntityType,
                Operation = item.Operation,
                Payload = item.Payload
            });
            await _db.SaveChangesAsync();
        }
        finally { SequenceLock.Release(); }
    }
}
