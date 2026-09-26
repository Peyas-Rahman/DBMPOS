using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SupplierLedger : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string? ReferenceNo { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string? Description { get; set; }
    public Guid? UserId { get; set; }
    public Supplier Supplier { get; set; } = null!;
}
