using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class ProductVariant : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public string VariantCode { get; set; } = null!;
    public string? Barcode { get; set; }
    public string VariantName { get; set; } = null!;
    public string? SKU { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MRP { get; set; }
    public Product Product { get; set; } = null!;
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
}