using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class ProductAttributeValue : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid ProductVariantId { get; set; }
    public Guid ProductAttributeId { get; set; }
    public string Value { get; set; } = null!;
    public ProductVariant ProductVariant { get; set; } = null!;
    public ProductAttribute ProductAttribute { get; set; } = null!;
}