using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class BranchServer : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Guid BranchId { get; set; }

    public string ServerCode { get; set; } = null!;

    public string ServerName { get; set; } = null!;

    public string? MachineName { get; set; }

    public string? LocalIpAddress { get; set; }

    public int ApiPort { get; set; } = 5000;

    public string? SoftwareVersion { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public int Status { get; set; }

    public Company Company { get; set; } = null!;

    public Branch Branch { get; set; } = null!;

    public ICollection<PosDevice> PosDevices { get; set; }
        = new List<PosDevice>();
}