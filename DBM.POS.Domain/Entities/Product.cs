using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Product : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? UnitId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MRP { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinStockLevel { get; set; }
    public bool TrackBatch { get; set; }
    public bool TrackExpiry { get; set; }
    public bool AllowNegativeStock { get; set; }
    public Company Company { get; set; } = null!;
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}