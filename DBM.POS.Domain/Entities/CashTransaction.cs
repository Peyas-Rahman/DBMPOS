using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class CashTransaction : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? PosSessionId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
}
