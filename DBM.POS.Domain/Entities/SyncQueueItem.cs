using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SyncQueueItem : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? BranchServerId { get; set; }
    public Guid TransactionId { get; set; }
    public string EntityType { get; set; } = null!;
    public string Operation { get; set; } = "Upsert";
    public string Payload { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string? SyncBatchId { get; set; }
}
