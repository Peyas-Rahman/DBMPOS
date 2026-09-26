using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class ProductAttribute : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public ICollection<ProductAttributeValue> Values { get; set; } = new List<ProductAttributeValue>();
}