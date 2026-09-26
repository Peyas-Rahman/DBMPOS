using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class SyncChange : BaseEntity
{
    public long Sequence { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid TransactionId { get; set; }
    public string EntityType { get; set; } = null!;
    public string Operation { get; set; } = "Upsert";
    public string Payload { get; set; } = null!;
}
