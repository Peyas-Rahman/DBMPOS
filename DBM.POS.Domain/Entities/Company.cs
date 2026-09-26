using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class Company : BaseEntity
{
    public string CompanyCode { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? BusinessType { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? LogoUrl { get; set; }

    public string CurrencyCode { get; set; } = "BDT";

    public string TimeZone { get; set; } = "Asia/Dhaka";

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}