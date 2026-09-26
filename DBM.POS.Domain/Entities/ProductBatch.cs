using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class ProductBatch : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid WarehouseId { get; set; }
    public string BatchNo { get; set; } = null!;
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal CostPrice { get; set; }
    public decimal MRP { get; set; }
    public decimal Quantity { get; set; }
}
