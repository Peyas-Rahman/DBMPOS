using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal LineTotal { get; set; }
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}