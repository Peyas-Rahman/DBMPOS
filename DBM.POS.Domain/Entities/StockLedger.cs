using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class StockLedger : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string TransactionType { get; set; } = null!;
    public Guid ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}