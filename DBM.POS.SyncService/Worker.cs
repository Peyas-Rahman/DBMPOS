using System.Text;
using System.Text.Json;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DBM.POS.SyncService;

public sealed class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<Worker> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Worker(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Clamp(_config.GetValue<int?>("Sync:IntervalSeconds") ?? 10, 5, 3600);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncOnceAsync(stoppingToken); }
            catch (Exception ex) { _logger.LogError(ex, "DBM POS sync cycle failed."); }
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken ct)
    {
        var central = (_config["Sync:CentralUrl"] ?? "").TrimEnd('/');
        var key = _config["Sync:ApiKey"] ?? "";
        if (!Uri.TryCreate(central, UriKind.Absolute, out _)) return;
        if (!Guid.TryParse(_config["Sync:CompanyId"], out var companyId) || !Guid.TryParse(_config["Sync:BranchId"], out var branchId)) return;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<POSDbContext>();
        await PushLocalQueueAsync(db, central, key, companyId, branchId, ct);
        await PullCentralChangesAsync(db, central, key, companyId, branchId, ct);
    }

    private async Task PushLocalQueueAsync(POSDbContext db, string central, string key, Guid companyId, Guid branchId, CancellationToken ct)
    {
        var rows = await db.SyncQueueItems.Where(x => x.CompanyId == companyId && x.BranchId == branchId && (x.Status == "Pending" || x.Status == "Failed"))
            .OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        if (rows.Count == 0) return;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Sync-Key");
        client.DefaultRequestHeaders.Add("X-Sync-Key", key);
        var items = rows.Select(x => new { x.TransactionId, x.CompanyId, x.BranchId, x.BranchServerId, x.EntityType, x.Operation, x.CreatedAt, x.Payload }).ToList();
        foreach (var r in rows) { r.RetryCount++; r.LastAttemptAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);

        var content = new StringContent(JsonSerializer.Serialize(new { items }), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{central}/api/sync/push", content, ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(ct);
            foreach (var r in rows) r.LastError = message.Length > 1900 ? message[..1900] : message;
            await db.SaveChangesAsync(ct);
            return;
        }
        var result = JsonSerializer.Deserialize<PushResult>(await response.Content.ReadAsStringAsync(ct), JsonOptions);
        var accepted = result?.Accepted ?? [];
        foreach (var r in rows.Where(x => accepted.Contains(x.TransactionId)))
        {
            r.Status = "Synced"; r.SyncedAt = DateTime.UtcNow; r.LastError = null;
        }
        foreach (var conflict in result?.Conflicts ?? [])
        {
            var row = rows.FirstOrDefault(x => x.TransactionId == conflict.TransactionId);
            if (row != null) { row.Status = "Failed"; row.LastError = conflict.Message; }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task PullCentralChangesAsync(POSDbContext db, string central, string key, Guid companyId, Guid branchId, CancellationToken ct)
    {
        var cursor = await db.SyncCursors.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == branchId && x.Scope == "Central", ct);
        if (cursor == null)
        {
            cursor = new SyncCursor { CompanyId = companyId, BranchId = branchId, Scope = "Central", LastSequence = 0 };
            db.SyncCursors.Add(cursor); await db.SaveChangesAsync(ct);
        }
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Remove("X-Sync-Key"); client.DefaultRequestHeaders.Add("X-Sync-Key", key);
        var url = $"{central}/api/sync/changes?companyId={companyId}&branchId={branchId}&afterSequence={cursor.LastSequence}&take=100";
        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return;
        var changes = JsonSerializer.Deserialize<List<ChangeDto>>(await response.Content.ReadAsStringAsync(ct), JsonOptions) ?? [];
        foreach (var change in changes.OrderBy(x => x.Sequence))
        {
            try { await ApplyChangeAsync(db, change, ct); cursor.LastSequence = change.Sequence; cursor.LastSyncAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to apply sync change {TransactionId} {EntityType}", change.TransactionId, change.EntityType); }
        }
    }

    private static async Task ApplyChangeAsync(POSDbContext db, ChangeDto item, CancellationToken ct)
    {
        switch (item.EntityType)
        {
            case "Sale": if (!await db.Sales.AnyAsync(x => x.Id == item.TransactionId, ct)) db.Sales.Add(JsonSerializer.Deserialize<Sale>(item.Payload, JsonOptions)!); break;
            case "Purchase": if (!await db.Purchases.AnyAsync(x => x.Id == item.TransactionId, ct)) db.Purchases.Add(JsonSerializer.Deserialize<Purchase>(item.Payload, JsonOptions)!); break;
            case "StockLedger": await ApplyStockLedgerAsync(db, JsonSerializer.Deserialize<StockLedger>(item.Payload, JsonOptions)!, ct); break;
            case "CustomerLedger": if (!await db.CustomerLedgers.AnyAsync(x => x.Id == item.TransactionId, ct)) db.CustomerLedgers.Add(JsonSerializer.Deserialize<CustomerLedger>(item.Payload, JsonOptions)!); break;
            case "SupplierLedger": if (!await db.SupplierLedgers.AnyAsync(x => x.Id == item.TransactionId, ct)) db.SupplierLedgers.Add(JsonSerializer.Deserialize<SupplierLedger>(item.Payload, JsonOptions)!); break;
            case "Expense": if (!await db.Expenses.AnyAsync(x => x.Id == item.TransactionId, ct)) db.Expenses.Add(JsonSerializer.Deserialize<Expense>(item.Payload, JsonOptions)!); break;
            case "CashTransaction": if (!await db.CashTransactions.AnyAsync(x => x.Id == item.TransactionId, ct)) db.CashTransactions.Add(JsonSerializer.Deserialize<CashTransaction>(item.Payload, JsonOptions)!); break;
            case "Payment": if (!await db.Payments.AnyAsync(x => x.Id == item.TransactionId, ct)) db.Payments.Add(JsonSerializer.Deserialize<Payment>(item.Payload, JsonOptions)!); break;
            case "Customer": await UpsertAsync(db.Customers, JsonSerializer.Deserialize<Customer>(item.Payload, JsonOptions)!, ct); break;
            case "Supplier": await UpsertAsync(db.Suppliers, JsonSerializer.Deserialize<Supplier>(item.Payload, JsonOptions)!, ct); break;
            case "Product": await UpsertAsync(db.Products, JsonSerializer.Deserialize<Product>(item.Payload, JsonOptions)!, ct); break;
            case "ProductVariant": await UpsertAsync(db.ProductVariants, JsonSerializer.Deserialize<ProductVariant>(item.Payload, JsonOptions)!, ct); break;
            case "Category": await UpsertAsync(db.Categories, JsonSerializer.Deserialize<Category>(item.Payload, JsonOptions)!, ct); break;
            case "Brand": await UpsertAsync(db.Brands, JsonSerializer.Deserialize<Brand>(item.Payload, JsonOptions)!, ct); break;
            case "Unit": await UpsertAsync(db.Units, JsonSerializer.Deserialize<Unit>(item.Payload, JsonOptions)!, ct); break;
        }
    }

    private static async Task ApplyStockLedgerAsync(POSDbContext db, StockLedger source, CancellationToken ct)
    {
        if (await db.StockLedgers.AnyAsync(x => x.Id == source.Id, ct)) return;
        var balance = await db.StockBalances.FirstOrDefaultAsync(x => x.CompanyId == source.CompanyId && x.WarehouseId == source.WarehouseId && x.ProductId == source.ProductId && x.VariantId == source.VariantId, ct);
        var delta = source.QuantityIn - source.QuantityOut;
        if (balance == null) { balance = new StockBalance { CompanyId = source.CompanyId, WarehouseId = source.WarehouseId, ProductId = source.ProductId, VariantId = source.VariantId, Quantity = 0, AverageCost = source.UnitCost }; db.StockBalances.Add(balance); }
        var old = balance.Quantity; var next = old + delta;
        if (delta > 0 && next > 0) balance.AverageCost = ((old * balance.AverageCost) + (delta * source.UnitCost)) / next;
        balance.Quantity = next; balance.UpdatedAt = DateTime.UtcNow; db.StockLedgers.Add(source);
    }

    private static async Task UpsertAsync<TEntity>(DbSet<TEntity> set, TEntity entity, CancellationToken ct) where TEntity : class
    {
        var prop = typeof(TEntity).GetProperty("Id") ?? throw new InvalidOperationException("Entity has no Id.");
        var id = (Guid)prop.GetValue(entity)!;
        var existing = await set.FindAsync(new object[] { id }, ct);
        if (existing == null) set.Add(entity); else set.Entry(existing).CurrentValues.SetValues(entity);
    }

    private sealed record PushResult(List<Guid> Accepted, List<ConflictDto> Conflicts);
    private sealed record ConflictDto(Guid TransactionId, string EntityType, string Message);
    private sealed record ChangeDto(long Sequence, Guid TransactionId, Guid CompanyId, Guid BranchId, string EntityType, string Operation, string Payload, DateTime CreatedAt);
}
