using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Category : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string? Description { get; set; }
    public Company Company { get; set; } = null!;
    public Category? ParentCategory { get; set; }
}