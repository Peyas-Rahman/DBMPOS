using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class Branch : BaseEntity
{
    public Guid CompanyId { get; set; }

    public string BranchCode { get; set; } = null!;

    public string BranchName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool IsHeadOffice { get; set; }

    public Company Company { get; set; } = null!;

    public ICollection<Warehouse> Warehouses { get; set; }
        = new List<Warehouse>();

    public ICollection<BranchServer> BranchServers { get; set; }
        = new List<BranchServer>();

    public ICollection<PosDevice> PosDevices { get; set; }
        = new List<PosDevice>();
}