using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Brand : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string BrandCode { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public Company Company { get; set; } = null!;
}