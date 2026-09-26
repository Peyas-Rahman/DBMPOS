using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SyncConflict : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TransactionId { get; set; }
    public string EntityType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = "Open";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
