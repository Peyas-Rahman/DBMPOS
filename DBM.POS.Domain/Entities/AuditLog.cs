using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class AuditLog : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string Module { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? EntityName { get; set; }
    public Guid? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? MachineName { get; set; }
}
