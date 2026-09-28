using DBM.POS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DBM.POS.Infrastructure.Data;

public class POSDbContext : DbContext
{
    public POSDbContext(DbContextOptions<POSDbContext> options)
        : base(options)
    {
    }

    public bool SyncJournalDisabled { get; set; }
    public Guid? SyncBranchId { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (SyncJournalDisabled)
            return base.SaveChanges(acceptAllChangesOnSuccess);

        var candidates = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        result += WriteSyncJournalAsync(candidates).GetAwaiter().GetResult();
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (SyncJournalDisabled) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        var candidates = ChangeTracker.Entries().Where(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        result += await WriteSyncJournalAsync(candidates, cancellationToken);
        return result;
    }

    private async Task<int> WriteSyncJournalAsync(IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> changedEntries, CancellationToken cancellationToken = default)
    {
        var entries = changedEntries
            .Where(e => e.Entity is not SyncQueueItem && e.Entity is not SyncChange && e.Entity is not SyncConflict && e.Entity is not SyncCursor)
            .Select(e => e.Entity)
            .Where(e => e is Sale || e is Purchase || e is SaleItem || e is PurchaseItem || e is Payment || e is StockLedger || e is CustomerLedger || e is SupplierLedger || e is Expense || e is CashTransaction || e is Customer || e is Supplier || e is Product || e is ProductVariant || e is Category || e is Brand || e is Unit || e is StockTransfer || e is StockTransferItem)
            .ToList();
        if (entries.Count == 0) return 0;

        var queue = new List<SyncQueueItem>();
        foreach (var entity in entries)
        {
            var id = (Guid)(entity.GetType().GetProperty("Id")?.GetValue(entity) ?? Guid.Empty);
            if (id == Guid.Empty) continue;
            var companyId = (Guid?)(entity.GetType().GetProperty("CompanyId")?.GetValue(entity)) ?? Guid.Empty;
            var branchId = (Guid?)(entity.GetType().GetProperty("BranchId")?.GetValue(entity)) ?? SyncBranchId ?? Guid.Empty;
            if (entity is StockLedger ledger)
                branchId = await Warehouses.Where(x => x.Id == ledger.WarehouseId).Select(x => (Guid?)x.BranchId).FirstOrDefaultAsync(cancellationToken) ?? Guid.Empty;
            if (entity is SaleItem || entity is PurchaseItem || entity is StockTransferItem) continue;
            if (companyId == Guid.Empty) continue;
            var entityType = entity.GetType().Name;
            if (entityType is "Sale" or "Purchase" or "StockLedger" or "CustomerLedger" or "SupplierLedger" or "Expense" or "CashTransaction")
            {
                if (await SyncQueueItems.AnyAsync(x => x.TransactionId == id && x.EntityType == entityType, cancellationToken)) continue;
            }
            var payload = System.Text.Json.JsonSerializer.Serialize(entity, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web) { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles });
            queue.Add(new SyncQueueItem { CompanyId = companyId, BranchId = branchId, TransactionId = id, EntityType = entityType, Operation = "Upsert", Payload = payload, Status = "Pending" });
        }
        if (queue.Count == 0) return 0;
        SyncJournalDisabled = true;
        try { SyncQueueItems.AddRange(queue); return await base.SaveChangesAsync(cancellationToken); }
        finally { SyncJournalDisabled = false; }
    }

    // Organization
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<BranchServer> BranchServers => Set<BranchServer>();
    public DbSet<PosDevice> PosDevices => Set<PosDevice>();

    // Security
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Business / Master Data
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<CustomerLedger> CustomerLedgers => Set<CustomerLedger>();
    public DbSet<SupplierLedger> SupplierLedgers => Set<SupplierLedger>();
    public DbSet<PosSession> PosSessions => Set<PosSession>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    // Transactions / Inventory
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<SyncQueueItem> SyncQueueItems => Set<SyncQueueItem>();
    public DbSet<SyncChange> SyncChanges => Set<SyncChange>();
    public DbSet<SyncConflict> SyncConflicts => Set<SyncConflict>();
    public DbSet<SyncCursor> SyncCursors => Set<SyncCursor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCompany(modelBuilder);
        ConfigureBranch(modelBuilder);
        ConfigureWarehouse(modelBuilder);
        ConfigureBranchServer(modelBuilder);
        ConfigurePosDevice(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigurePermission(modelBuilder);
        ConfigureUserRole(modelBuilder);
        ConfigureUserBranch(modelBuilder);
        ConfigureRolePermission(modelBuilder);
        ConfigureBusinessModels(modelBuilder);
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Company>();

        entity.ToTable("Companies");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.CompanyCode)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.BusinessType)
            .HasMaxLength(100);

        entity.Property(x => x.Phone)
            .HasMaxLength(30);

        entity.Property(x => x.Email)
            .HasMaxLength(150);

        entity.Property(x => x.Address)
            .HasMaxLength(500);

        entity.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        entity.Property(x => x.CurrencyCode)
            .HasMaxLength(10)
            .IsRequired();

        entity.Property(x => x.TimeZone)
            .HasMaxLength(50)
            .IsRequired();

        entity.HasIndex(x => x.CompanyCode)
            .IsUnique();
    }

    private static void ConfigureBranch(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Branch>();

        entity.ToTable("Branches");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.BranchCode)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.BranchName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Phone)
            .HasMaxLength(30);

        entity.Property(x => x.Address)
            .HasMaxLength(500);

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.BranchCode
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany(x => x.Branches)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureWarehouse(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Warehouse>();

        entity.ToTable("Warehouses");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.WarehouseCode)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(x => x.WarehouseName)
            .HasMaxLength(200)
            .IsRequired();

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.BranchId,
            x.WarehouseCode
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Branch)
            .WithMany(x => x.Warehouses)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBranchServer(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<BranchServer>();

        entity.ToTable("BranchServers");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.ServerCode)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.ServerName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.MachineName)
            .HasMaxLength(200);

        entity.Property(x => x.LocalIpAddress)
            .HasMaxLength(50);

        entity.Property(x => x.SoftwareVersion)
            .HasMaxLength(50);

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.BranchId,
            x.ServerCode
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Branch)
            .WithMany(x => x.BranchServers)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePosDevice(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<PosDevice>();

        entity.ToTable("PosDevices");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.DeviceCode)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.DeviceName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.MacAddress)
            .HasMaxLength(100);

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.BranchId,
            x.DeviceCode
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Branch)
            .WithMany(x => x.PosDevices)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Server)
            .WithMany(x => x.PosDevices)
            .HasForeignKey(x => x.ServerId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<User>();

        entity.ToTable("Users");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Username)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasMaxLength(150)
            .IsRequired();

        entity.Property(x => x.FullName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Phone)
            .HasMaxLength(30);

        entity.Property(x => x.PasswordHash)
            .IsRequired();

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.Username
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRole(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Role>();

        entity.ToTable("Roles");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.RoleName)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Description)
            .HasMaxLength(500);

        entity.HasIndex(x => new
        {
            x.CompanyId,
            x.RoleName
        })
        .IsUnique();

        entity.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurePermission(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Permission>();

        entity.ToTable("Permissions");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.PermissionCode)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.PermissionName)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Module)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.Description)
            .HasMaxLength(500);

        entity.HasIndex(x => x.PermissionCode)
            .IsUnique();
    }

    private static void ConfigureUserRole(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserRole>();

        entity.ToTable("UserRoles");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new
        {
            x.UserId,
            x.RoleId,
            x.BranchId
        })
        .IsUnique();

        entity.HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureUserBranch(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserBranch>();

        entity.ToTable("UserBranches");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new
        {
            x.UserId,
            x.BranchId
        })
        .IsUnique();

        entity.HasOne(x => x.User)
            .WithMany(x => x.UserBranches)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<RolePermission>();

        entity.ToTable("RolePermissions");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new
        {
            x.RoleId,
            x.PermissionId
        })
        .IsUnique();

        entity.HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureBusinessModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories"); e.HasKey(x => x.Id);
            e.Property(x => x.CategoryCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.CategoryName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasIndex(x => new { x.CompanyId, x.CategoryCode }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ParentCategory).WithMany().HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Brand>(e =>
        {
            e.ToTable("Brands"); e.HasKey(x => x.Id);
            e.Property(x => x.BrandCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.BrandName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.BrandCode }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Unit>(e =>
        {
            e.ToTable("Units"); e.HasKey(x => x.Id);
            e.Property(x => x.UnitCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.UnitName).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConversionFactor).HasPrecision(18,6);
            e.HasIndex(x => new { x.CompanyId, x.UnitCode }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products"); e.HasKey(x => x.Id);
            e.Property(x => x.ProductCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Barcode).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.CostPrice).HasPrecision(18,2);
            e.Property(x => x.SalePrice).HasPrecision(18,2);
            e.Property(x => x.MRP).HasPrecision(18,2);
            e.Property(x => x.TaxPercent).HasPrecision(8,2);
            e.Property(x => x.MinStockLevel).HasPrecision(18,3);
            e.HasIndex(x => new { x.CompanyId, x.ProductCode }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Barcode });
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("ProductVariants"); e.HasKey(x => x.Id);
            e.Property(x => x.VariantCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.VariantName).HasMaxLength(300).IsRequired();
            e.Property(x => x.Barcode).HasMaxLength(100);
            e.Property(x => x.SKU).HasMaxLength(100);
            e.Property(x => x.CostPrice).HasPrecision(18,2);
            e.Property(x => x.SalePrice).HasPrecision(18,2);
            e.Property(x => x.MRP).HasPrecision(18,2);
            e.HasIndex(x => new { x.CompanyId, x.VariantCode }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Barcode });
            e.HasOne(x => x.Product).WithMany(x => x.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductAttribute>(e =>
        {
            e.ToTable("ProductAttributes"); e.HasKey(x => x.Id);
            e.Property(x => x.AttributeCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.AttributeName).HasMaxLength(100).IsRequired();
            e.Property(x => x.DefaultValues).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.AttributeCode }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ProductAttributeValue>(e =>
        {
            e.ToTable("ProductAttributeValues"); e.HasKey(x => x.Id);
            e.Property(x => x.Value).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ProductVariantId, x.ProductAttributeId }).IsUnique();
            e.HasOne(x => x.ProductVariant).WithMany(x => x.AttributeValues).HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProductAttribute).WithMany(x => x.Values).HasForeignKey(x => x.ProductAttributeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers"); e.HasKey(x => x.Id);
            e.Property(x => x.CustomerCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30); e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.CreditLimit).HasPrecision(18,2); e.Property(x => x.OpeningDue).HasPrecision(18,2);
            e.Property(x => x.DiscountPercent).HasPrecision(18, 2);
            e.HasIndex(x => new { x.CompanyId, x.CustomerCode }).IsUnique();
            e.HasIndex(x => new { x.CompanyId, x.Phone });
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Supplier>(e =>
        {
            e.ToTable("Suppliers"); e.HasKey(x => x.Id);
            e.Property(x => x.SupplierCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(30); e.Property(x => x.Email).HasMaxLength(150);
            e.Property(x => x.Address).HasMaxLength(500); e.Property(x => x.OpeningDue).HasPrecision(18,2);
            e.HasIndex(x => new { x.CompanyId, x.SupplierCode }).IsUnique();
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CustomerLedger>(e =>
        {
            e.ToTable("CustomerLedgers"); e.HasKey(x => x.Id);
            foreach (var p in new[] {"Debit","Credit","Balance"}) e.Property(p).HasPrecision(18,2);
            e.Property(x=>x.TransactionType).HasMaxLength(50).IsRequired(); e.Property(x=>x.ReferenceNo).HasMaxLength(100); e.Property(x=>x.Description).HasMaxLength(500);
            e.HasIndex(x=>new{x.CompanyId,x.CustomerId,x.TransactionDate});
            e.HasOne(x=>x.Customer).WithMany().HasForeignKey(x=>x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SupplierLedger>(e =>
        {
            e.ToTable("SupplierLedgers"); e.HasKey(x => x.Id);
            foreach (var p in new[] {"Debit","Credit","Balance"}) e.Property(p).HasPrecision(18,2);
            e.Property(x=>x.TransactionType).HasMaxLength(50).IsRequired(); e.Property(x=>x.ReferenceNo).HasMaxLength(100); e.Property(x=>x.Description).HasMaxLength(500);
            e.HasIndex(x=>new{x.CompanyId,x.SupplierId,x.TransactionDate});
            e.HasOne(x=>x.Supplier).WithMany().HasForeignKey(x=>x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PosSession>(e =>
        {
            e.ToTable("PosSessions"); e.HasKey(x=>x.Id);
            foreach(var p in new[]{"OpeningCash","CashSales","CashIn","CashOut","CashRefund","ExpectedCash","ActualCash","Difference"}) e.Property(p).HasPrecision(18,2);
            e.Property(x=>x.Status).HasMaxLength(20).IsRequired(); e.HasIndex(x=>new{x.CompanyId,x.BranchId,x.Status});
        });
        modelBuilder.Entity<Expense>(e =>
        {
            e.ToTable("Expenses"); e.HasKey(x=>x.Id); e.Property(x=>x.ExpenseNo).HasMaxLength(50).IsRequired(); e.Property(x=>x.Category).HasMaxLength(100).IsRequired(); e.Property(x=>x.Description).HasMaxLength(500); e.Property(x=>x.Amount).HasPrecision(18,2); e.Property(x=>x.PaymentMethod).HasMaxLength(30).IsRequired(); e.HasIndex(x=>new{x.CompanyId,x.ExpenseNo}).IsUnique();
        });
        modelBuilder.Entity<ProductBatch>(e =>
        {
            e.ToTable("ProductBatches"); e.HasKey(x=>x.Id); e.Property(x=>x.BatchNo).HasMaxLength(100).IsRequired(); e.Property(x=>x.CostPrice).HasPrecision(18,2); e.Property(x=>x.MRP).HasPrecision(18,2); e.Property(x=>x.Quantity).HasPrecision(18,3); e.HasIndex(x=>new{x.CompanyId,x.ProductId,x.WarehouseId,x.BatchNo});
        });
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs"); e.HasKey(x=>x.Id); e.Property(x=>x.Module).HasMaxLength(100).IsRequired(); e.Property(x=>x.Action).HasMaxLength(100).IsRequired(); e.Property(x=>x.EntityName).HasMaxLength(100); e.Property(x=>x.IpAddress).HasMaxLength(100); e.Property(x=>x.MachineName).HasMaxLength(200);
        });
        modelBuilder.Entity<CashTransaction>(e =>
        {
            e.ToTable("CashTransactions"); e.HasKey(x=>x.Id); e.Property(x=>x.TransactionType).HasMaxLength(50).IsRequired(); e.Property(x=>x.Amount).HasPrecision(18,2); e.Property(x=>x.PaymentMethod).HasMaxLength(30).IsRequired(); e.Property(x=>x.ReferenceNo).HasMaxLength(100); e.Property(x=>x.Description).HasMaxLength(500);
        });
        modelBuilder.Entity<PaymentMethod>(e =>
        {
            e.ToTable("PaymentMethods"); e.HasKey(x=>x.Id); e.Property(x=>x.Code).HasMaxLength(30).IsRequired(); e.Property(x=>x.Name).HasMaxLength(100).IsRequired(); e.HasIndex(x=>new{x.CompanyId,x.Code}).IsUnique();
        });

        modelBuilder.Entity<Purchase>(e =>
        {
            e.ToTable("Purchases"); e.HasKey(x => x.Id);
            e.Property(x => x.PurchaseNo).HasMaxLength(50).IsRequired();
            e.Property(x => x.PurchaseDate).IsRequired();
            foreach (var p in new[] {"SubTotal","Discount","Tax","GrandTotal","PaidAmount","DueAmount"}) e.Property(p).HasPrecision(18,2);
            e.HasIndex(x => new { x.CompanyId, x.PurchaseNo }).IsUnique();
            e.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PurchaseItem>(e =>
        {
            e.ToTable("PurchaseItems"); e.HasKey(x => x.Id);
            foreach (var p in new[] {"Quantity","UnitCost","Discount","Tax","LineTotal"}) e.Property(p).HasPrecision(18,3);
            e.HasOne(x => x.Purchase).WithMany(x => x.Items).HasForeignKey(x => x.PurchaseId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Sale>(e =>
        {
            e.ToTable("Sales"); e.HasKey(x => x.Id);
            e.Property(x => x.InvoiceNo).HasMaxLength(50).IsRequired();
            e.Property(x => x.PaymentStatus).HasMaxLength(30).IsRequired();
            foreach (var p in new[] {"SubTotal","Discount","Tax","GrandTotal","PaidAmount","DueAmount"}) e.Property(p).HasPrecision(18,2);
            e.HasIndex(x => new { x.CompanyId, x.InvoiceNo }).IsUnique();
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SaleItem>(e =>
        {
            e.ToTable("SaleItems"); e.HasKey(x => x.Id);
            foreach (var p in new[] {"Quantity","UnitPrice","UnitCost","Discount","Tax","LineTotal"}) e.Property(p).HasPrecision(18,3);
            e.HasOne(x => x.Sale).WithMany(x => x.Items).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments"); e.HasKey(x => x.Id);
            e.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18,2);
            e.Property(x => x.ReferenceNo).HasMaxLength(100);
            e.HasOne(x => x.Sale).WithMany(x => x.Payments).HasForeignKey(x => x.SaleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StockBalance>(e =>
        {
            e.ToTable("StockBalances"); e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasPrecision(18,3); e.Property(x => x.AverageCost).HasPrecision(18,2);
            e.HasIndex(x => new { x.WarehouseId, x.ProductId, x.VariantId }).IsUnique();
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockLedger>(e =>
        {
            e.ToTable("StockLedgers"); e.HasKey(x => x.Id);
            e.Property(x => x.TransactionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ReferenceNo).HasMaxLength(100);
            e.Property(x => x.QuantityIn).HasPrecision(18,3); e.Property(x => x.QuantityOut).HasPrecision(18,3);
            e.Property(x => x.BalanceAfter).HasPrecision(18,3); e.Property(x => x.UnitCost).HasPrecision(18,2);
            e.HasIndex(x => new { x.WarehouseId, x.ProductId, x.TransactionDate });
        });
        modelBuilder.Entity<StockTransfer>(e =>
        {
            e.ToTable("StockTransfers"); e.HasKey(x => x.Id);
            e.Property(x => x.TransferNo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.TransferNo }).IsUnique();
        });
        modelBuilder.Entity<SyncQueueItem>(e =>
        {
            e.ToTable("SyncQueueItems"); e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Operation).HasMaxLength(30).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.LastError).HasMaxLength(2000);
            e.Property(x => x.SyncBatchId).HasMaxLength(100);
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.CreatedAt });
        });
        modelBuilder.Entity<SyncChange>(e =>
        {
            e.ToTable("SyncChanges"); e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Operation).HasMaxLength(30).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Sequence }).IsUnique();
        });
        modelBuilder.Entity<SyncConflict>(e =>
        {
            e.ToTable("SyncConflicts"); e.HasKey(x => x.Id);
            e.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            e.Property(x => x.Payload).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status });
        });
        modelBuilder.Entity<SyncCursor>(e =>
        {
            e.ToTable("SyncCursors"); e.HasKey(x => x.Id);
            e.Property(x => x.Scope).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.BranchId, x.Scope }).IsUnique();
        });

        modelBuilder.Entity<StockTransferItem>(e =>
        {
            e.ToTable("StockTransferItems"); e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasPrecision(18,3);
            e.HasOne(x => x.StockTransfer).WithMany(x => x.Items).HasForeignKey(x => x.StockTransferId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}