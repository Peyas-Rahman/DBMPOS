using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Payment : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid SaleId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public Sale Sale { get; set; } = null!;
}