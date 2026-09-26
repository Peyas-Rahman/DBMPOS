using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Supplier : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string SupplierCode { get; set; } = null!;
    public string SupplierName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal OpeningDue { get; set; }
    public Company Company { get; set; } = null!;
}