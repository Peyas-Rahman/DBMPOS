using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SyncCursor : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Scope { get; set; } = "Central";
    public long LastSequence { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
