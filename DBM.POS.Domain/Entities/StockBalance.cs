using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class StockBalance : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public Product Product { get; set; } = null!;
    public Warehouse Warehouse { get; set; } = null!;
}