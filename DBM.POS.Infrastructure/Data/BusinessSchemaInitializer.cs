using Microsoft.EntityFrameworkCore;

namespace DBM.POS.Infrastructure.Data;

public static class BusinessSchemaInitializer
{
    public static async Task EnsureAsync(POSDbContext db)
    {
        var sql = new[]
        {
@"IF OBJECT_ID(N'[Categories]',N'U') IS NULL CREATE TABLE [Categories](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[ParentCategoryId] uniqueidentifier NULL,[CategoryCode] nvarchar(50) NOT NULL,[CategoryName] nvarchar(200) NOT NULL,[Description] nvarchar(500) NULL);",
@"IF OBJECT_ID(N'[Brands]',N'U') IS NULL CREATE TABLE [Brands](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[BrandCode] nvarchar(50) NOT NULL,[BrandName] nvarchar(200) NOT NULL);",
@"IF OBJECT_ID(N'[Units]',N'U') IS NULL CREATE TABLE [Units](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[UnitCode] nvarchar(50) NOT NULL,[UnitName] nvarchar(100) NOT NULL,[ConversionFactor] decimal(18,6) NOT NULL);",
@"IF OBJECT_ID(N'[Products]',N'U') IS NULL CREATE TABLE [Products](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[CategoryId] uniqueidentifier NULL,[BrandId] uniqueidentifier NULL,[UnitId] uniqueidentifier NULL,
[ProductCode] nvarchar(100) NOT NULL,[ProductName] nvarchar(300) NOT NULL,[Barcode] nvarchar(100) NULL,[Description] nvarchar(1000) NULL,
[CostPrice] decimal(18,2) NOT NULL,[SalePrice] decimal(18,2) NOT NULL,[MRP] decimal(18,2) NOT NULL,[TaxPercent] decimal(8,2) NOT NULL,[MinStockLevel] decimal(18,3) NOT NULL,
[TrackBatch] bit NOT NULL,[TrackExpiry] bit NOT NULL,[AllowNegativeStock] bit NOT NULL);",
@"IF OBJECT_ID(N'[ProductVariants]',N'U') IS NULL CREATE TABLE [ProductVariants](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantCode] nvarchar(100) NOT NULL,[Barcode] nvarchar(100) NULL,[VariantName] nvarchar(300) NOT NULL,[SKU] nvarchar(100) NULL,
[CostPrice] decimal(18,2) NOT NULL,[SalePrice] decimal(18,2) NOT NULL,[MRP] decimal(18,2) NOT NULL);",
@"IF OBJECT_ID(N'[ProductAttributes]',N'U') IS NULL CREATE TABLE [ProductAttributes](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[AttributeCode] nvarchar(50) NOT NULL,[AttributeName] nvarchar(100) NOT NULL);",
@"IF OBJECT_ID(N'[ProductAttributeValues]',N'U') IS NULL CREATE TABLE [ProductAttributeValues](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[ProductVariantId] uniqueidentifier NOT NULL,[ProductAttributeId] uniqueidentifier NOT NULL,[Value] nvarchar(200) NOT NULL);",
@"IF OBJECT_ID(N'[Customers]',N'U') IS NULL CREATE TABLE [Customers](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[CustomerCode] nvarchar(50) NOT NULL,[CustomerName] nvarchar(200) NOT NULL,[Phone] nvarchar(30) NULL,[Email] nvarchar(150) NULL,[Address] nvarchar(500) NULL,[CreditLimit] decimal(18,2) NOT NULL,[OpeningDue] decimal(18,2) NOT NULL);",
@"IF OBJECT_ID(N'[Suppliers]',N'U') IS NULL CREATE TABLE [Suppliers](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[SupplierCode] nvarchar(50) NOT NULL,[SupplierName] nvarchar(200) NOT NULL,[Phone] nvarchar(30) NULL,[Email] nvarchar(150) NULL,[Address] nvarchar(500) NULL,[OpeningDue] decimal(18,2) NOT NULL);",
@"IF OBJECT_ID(N'[Purchases]',N'U') IS NULL CREATE TABLE [Purchases](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NOT NULL,[SupplierId] uniqueidentifier NOT NULL,
[PurchaseNo] nvarchar(50) NOT NULL,[PurchaseDate] datetime2 NOT NULL,[SubTotal] decimal(18,2) NOT NULL,[Discount] decimal(18,2) NOT NULL,[Tax] decimal(18,2) NOT NULL,[GrandTotal] decimal(18,2) NOT NULL,[PaidAmount] decimal(18,2) NOT NULL,[DueAmount] decimal(18,2) NOT NULL);",
@"IF OBJECT_ID(N'[PurchaseItems]',N'U') IS NULL CREATE TABLE [PurchaseItems](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[PurchaseId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[Quantity] decimal(18,3) NOT NULL,[UnitCost] decimal(18,3) NOT NULL,[Discount] decimal(18,3) NOT NULL,[Tax] decimal(18,3) NOT NULL,[LineTotal] decimal(18,3) NOT NULL);",
@"IF OBJECT_ID(N'[Sales]',N'U') IS NULL CREATE TABLE [Sales](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NOT NULL,[CustomerId] uniqueidentifier NULL,[PosDeviceId] uniqueidentifier NULL,[UserId] uniqueidentifier NULL,
[InvoiceNo] nvarchar(50) NOT NULL,[SaleDate] datetime2 NOT NULL,[SubTotal] decimal(18,2) NOT NULL,[Discount] decimal(18,2) NOT NULL,[Tax] decimal(18,2) NOT NULL,[GrandTotal] decimal(18,2) NOT NULL,[PaidAmount] decimal(18,2) NOT NULL,[DueAmount] decimal(18,2) NOT NULL,[PaymentStatus] nvarchar(30) NOT NULL);",
@"IF OBJECT_ID(N'[SaleItems]',N'U') IS NULL CREATE TABLE [SaleItems](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[SaleId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[Quantity] decimal(18,3) NOT NULL,[UnitPrice] decimal(18,3) NOT NULL,[UnitCost] decimal(18,3) NOT NULL,[Discount] decimal(18,3) NOT NULL,[Tax] decimal(18,3) NOT NULL,[LineTotal] decimal(18,3) NOT NULL);",
@"IF OBJECT_ID(N'[Payments]',N'U') IS NULL CREATE TABLE [Payments](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[SaleId] uniqueidentifier NOT NULL,[PaymentMethod] nvarchar(30) NOT NULL,[Amount] decimal(18,2) NOT NULL,[ReferenceNo] nvarchar(100) NULL,[PaymentDate] datetime2 NOT NULL);",
@"IF OBJECT_ID(N'[StockBalances]',N'U') IS NULL CREATE TABLE [StockBalances](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[Quantity] decimal(18,3) NOT NULL,[AverageCost] decimal(18,2) NOT NULL);",
@"IF OBJECT_ID(N'[StockLedgers]',N'U') IS NULL CREATE TABLE [StockLedgers](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[TransactionType] nvarchar(50) NOT NULL,[ReferenceId] uniqueidentifier NOT NULL,[ReferenceNo] nvarchar(100) NULL,[QuantityIn] decimal(18,3) NOT NULL,[QuantityOut] decimal(18,3) NOT NULL,[BalanceAfter] decimal(18,3) NOT NULL,[UnitCost] decimal(18,2) NOT NULL,[TransactionDate] datetime2 NOT NULL);",
@"IF OBJECT_ID(N'[StockTransfers]',N'U') IS NULL CREATE TABLE [StockTransfers](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[CompanyId] uniqueidentifier NOT NULL,[FromBranchId] uniqueidentifier NOT NULL,[FromWarehouseId] uniqueidentifier NOT NULL,[ToBranchId] uniqueidentifier NOT NULL,[ToWarehouseId] uniqueidentifier NOT NULL,[TransferNo] nvarchar(50) NOT NULL,[Status] nvarchar(30) NOT NULL,[TransferDate] datetime2 NOT NULL);",
@"IF OBJECT_ID(N'[StockTransferItems]',N'U') IS NULL CREATE TABLE [StockTransferItems](
[Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,
[StockTransferId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[Quantity] decimal(18,3) NOT NULL);"
        ,
@"IF COL_LENGTH(N'Customers',N'ContactPerson') IS NULL ALTER TABLE [Customers] ADD [ContactPerson] nvarchar(150) NULL;
IF COL_LENGTH(N'Customers',N'NID') IS NULL ALTER TABLE [Customers] ADD [NID] nvarchar(50) NULL;
IF COL_LENGTH(N'Customers',N'City') IS NULL ALTER TABLE [Customers] ADD [City] nvarchar(100) NULL;
IF COL_LENGTH(N'Customers',N'District') IS NULL ALTER TABLE [Customers] ADD [District] nvarchar(100) NULL;
IF COL_LENGTH(N'Customers',N'PostalCode') IS NULL ALTER TABLE [Customers] ADD [PostalCode] nvarchar(20) NULL;
IF COL_LENGTH(N'Customers',N'CustomerType') IS NULL ALTER TABLE [Customers] ADD [CustomerType] nvarchar(50) NULL;
IF COL_LENGTH(N'Customers',N'CreditDays') IS NULL ALTER TABLE [Customers] ADD [CreditDays] int NOT NULL CONSTRAINT [DF_Customers_CreditDays] DEFAULT(0);
IF COL_LENGTH(N'Customers',N'DiscountPercent') IS NULL ALTER TABLE [Customers] ADD [DiscountPercent] decimal(18,2) NOT NULL CONSTRAINT [DF_Customers_DiscountPercent] DEFAULT(0);
IF COL_LENGTH(N'Customers',N'Notes') IS NULL ALTER TABLE [Customers] ADD [Notes] nvarchar(1000) NULL;",
@"IF OBJECT_ID(N'[CustomerLedgers]',N'U') IS NULL CREATE TABLE [CustomerLedgers]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[CustomerId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NULL,[TransactionDate] datetime2 NOT NULL,[TransactionType] nvarchar(50) NOT NULL,[ReferenceId] uniqueidentifier NULL,[ReferenceNo] nvarchar(100) NULL,[Debit] decimal(18,2) NOT NULL,[Credit] decimal(18,2) NOT NULL,[Balance] decimal(18,2) NOT NULL,[Description] nvarchar(500) NULL,[UserId] uniqueidentifier NULL);",
@"IF OBJECT_ID(N'[SupplierLedgers]',N'U') IS NULL CREATE TABLE [SupplierLedgers]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[SupplierId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NULL,[TransactionDate] datetime2 NOT NULL,[TransactionType] nvarchar(50) NOT NULL,[ReferenceId] uniqueidentifier NULL,[ReferenceNo] nvarchar(100) NULL,[Debit] decimal(18,2) NOT NULL,[Credit] decimal(18,2) NOT NULL,[Balance] decimal(18,2) NOT NULL,[Description] nvarchar(500) NULL,[UserId] uniqueidentifier NULL);",
@"IF OBJECT_ID(N'[PosSessions]',N'U') IS NULL CREATE TABLE [PosSessions]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[WarehouseId] uniqueidentifier NULL,[PosDeviceId] uniqueidentifier NULL,[OpenedBy] uniqueidentifier NOT NULL,[ClosedBy] uniqueidentifier NULL,[OpenedAt] datetime2 NOT NULL,[ClosedAt] datetime2 NULL,[OpeningCash] decimal(18,2) NOT NULL,[CashSales] decimal(18,2) NOT NULL,[CashIn] decimal(18,2) NOT NULL,[CashOut] decimal(18,2) NOT NULL,[CashRefund] decimal(18,2) NOT NULL,[ExpectedCash] decimal(18,2) NOT NULL,[ActualCash] decimal(18,2) NOT NULL,[Difference] decimal(18,2) NOT NULL,[Status] nvarchar(20) NOT NULL);",
@"IF OBJECT_ID(N'[Expenses]',N'U') IS NULL CREATE TABLE [Expenses]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[ExpenseNo] nvarchar(50) NOT NULL,[ExpenseDate] datetime2 NOT NULL,[Category] nvarchar(100) NOT NULL,[Description] nvarchar(500) NULL,[Amount] decimal(18,2) NOT NULL,[PaymentMethod] nvarchar(30) NOT NULL,[ReferenceNo] nvarchar(100) NULL,[UserId] uniqueidentifier NULL);",
@"IF OBJECT_ID(N'[ProductBatches]',N'U') IS NULL CREATE TABLE [ProductBatches]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[ProductId] uniqueidentifier NOT NULL,[VariantId] uniqueidentifier NULL,[WarehouseId] uniqueidentifier NOT NULL,[BatchNo] nvarchar(100) NOT NULL,[ManufactureDate] datetime2 NULL,[ExpiryDate] datetime2 NULL,[CostPrice] decimal(18,2) NOT NULL,[MRP] decimal(18,2) NOT NULL,[Quantity] decimal(18,3) NOT NULL);",
@"IF OBJECT_ID(N'[AuditLogs]',N'U') IS NULL CREATE TABLE [AuditLogs]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[UserId] uniqueidentifier NULL,[Module] nvarchar(100) NOT NULL,[Action] nvarchar(100) NOT NULL,[EntityName] nvarchar(100) NULL,[EntityId] uniqueidentifier NULL,[OldValues] nvarchar(max) NULL,[NewValues] nvarchar(max) NULL,[IpAddress] nvarchar(100) NULL,[MachineName] nvarchar(200) NULL);",
@"IF OBJECT_ID(N'[CashTransactions]',N'U') IS NULL CREATE TABLE [CashTransactions]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[PosSessionId] uniqueidentifier NULL,[TransactionDate] datetime2 NOT NULL,[TransactionType] nvarchar(50) NOT NULL,[Amount] decimal(18,2) NOT NULL,[PaymentMethod] nvarchar(30) NOT NULL,[ReferenceNo] nvarchar(100) NULL,[Description] nvarchar(500) NULL,[UserId] uniqueidentifier NULL);",
@"IF OBJECT_ID(N'[PaymentMethods]',N'U') IS NULL CREATE TABLE [PaymentMethods]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[Code] nvarchar(30) NOT NULL,[Name] nvarchar(100) NOT NULL,[IsCash] bit NOT NULL,[IsDefault] bit NOT NULL);"
        ,
@"IF OBJECT_ID(N'[SyncQueueItems]',N'U') IS NULL CREATE TABLE [SyncQueueItems]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[BranchServerId] uniqueidentifier NULL,[TransactionId] uniqueidentifier NOT NULL,[EntityType] nvarchar(80) NOT NULL,[Operation] nvarchar(30) NOT NULL,[Payload] nvarchar(max) NOT NULL,[Status] nvarchar(30) NOT NULL,[RetryCount] int NOT NULL,[LastAttemptAt] datetime2 NULL,[LastError] nvarchar(2000) NULL,[SyncedAt] datetime2 NULL,[SyncBatchId] nvarchar(100) NULL);",
@"IF OBJECT_ID(N'[SyncChanges]',N'U') IS NULL CREATE TABLE [SyncChanges]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[Sequence] bigint NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[TransactionId] uniqueidentifier NOT NULL,[EntityType] nvarchar(80) NOT NULL,[Operation] nvarchar(30) NOT NULL,[Payload] nvarchar(max) NOT NULL);",
@"IF OBJECT_ID(N'[SyncConflicts]',N'U') IS NULL CREATE TABLE [SyncConflicts]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[TransactionId] uniqueidentifier NOT NULL,[EntityType] nvarchar(80) NOT NULL,[Payload] nvarchar(max) NOT NULL,[Reason] nvarchar(1000) NOT NULL,[Status] nvarchar(30) NOT NULL,[DetectedAt] datetime2 NOT NULL,[ResolvedAt] datetime2 NULL);",
@"IF OBJECT_ID(N'[SyncCursors]',N'U') IS NULL CREATE TABLE [SyncCursors]([Id] uniqueidentifier NOT NULL PRIMARY KEY,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsActive] bit NOT NULL,[CompanyId] uniqueidentifier NOT NULL,[BranchId] uniqueidentifier NOT NULL,[Scope] nvarchar(50) NOT NULL,[LastSequence] bigint NOT NULL,[LastSyncAt] datetime2 NULL);",
@"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SyncCursors_Company_Branch_Scope' AND object_id=OBJECT_ID('SyncCursors')) CREATE UNIQUE INDEX IX_SyncCursors_Company_Branch_Scope ON SyncCursors(CompanyId,BranchId,Scope);",
@"INSERT INTO CustomerLedgers(Id,CreatedAt,IsActive,CompanyId,CustomerId,BranchId,TransactionDate,TransactionType,ReferenceId,ReferenceNo,Debit,Credit,Balance,Description)
SELECT NEWID(),SYSUTCDATETIME(),1,s.CompanyId,s.CustomerId,s.BranchId,s.SaleDate,'Sale',s.Id,s.InvoiceNo,s.DueAmount,0,
       c.OpeningDue + (SELECT COALESCE(SUM(s2.DueAmount),0) FROM Sales s2 WHERE s2.CustomerId=s.CustomerId AND s2.DueAmount>0 AND (s2.SaleDate<s.SaleDate OR (s2.SaleDate=s.SaleDate AND s2.CreatedAt<=s.CreatedAt))), 'Historical credit sale'
FROM Sales s INNER JOIN Customers c ON c.Id=s.CustomerId
WHERE s.CustomerId IS NOT NULL AND s.DueAmount>0 AND NOT EXISTS(SELECT 1 FROM CustomerLedgers l WHERE l.ReferenceId=s.Id AND l.TransactionType='Sale');",
@"INSERT INTO SupplierLedgers(Id,CreatedAt,IsActive,CompanyId,SupplierId,BranchId,TransactionDate,TransactionType,ReferenceId,ReferenceNo,Debit,Credit,Balance,Description)
SELECT NEWID(),SYSUTCDATETIME(),1,p.CompanyId,p.SupplierId,p.BranchId,p.PurchaseDate,'Purchase',p.Id,p.PurchaseNo,p.DueAmount,0,
       s.OpeningDue + (SELECT COALESCE(SUM(p2.DueAmount),0) FROM Purchases p2 WHERE p2.SupplierId=p.SupplierId AND p2.DueAmount>0 AND (p2.PurchaseDate<p.PurchaseDate OR (p2.PurchaseDate=p.PurchaseDate AND p2.CreatedAt<=p.CreatedAt))), 'Historical credit purchase'
FROM Purchases p INNER JOIN Suppliers s ON s.Id=p.SupplierId
WHERE p.DueAmount>0 AND NOT EXISTS(SELECT 1 FROM SupplierLedgers l WHERE l.ReferenceId=p.Id AND l.TransactionType='Purchase');"
        };
        foreach(var command in sql) await db.Database.ExecuteSqlRawAsync(command);
        await db.Database.ExecuteSqlRawAsync(@"IF COL_LENGTH(N'[ProductAttributes]', N'DefaultValues') IS NULL ALTER TABLE [ProductAttributes] ADD [DefaultValues] nvarchar(1000) NOT NULL CONSTRAINT [DF_ProductAttributes_DefaultValues] DEFAULT N'';");
    }
}