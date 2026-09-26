using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Customer : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string CustomerCode { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ContactPerson { get; set; }
    public string? NID { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? CustomerType { get; set; }
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal OpeningDue { get; set; }
    public string? Notes { get; set; }
    public Company Company { get; set; } = null!;
}
