using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Expense : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string ExpenseNo { get; set; } = null!;
    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public string Category { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? ReferenceNo { get; set; }
    public Guid? UserId { get; set; }
}
