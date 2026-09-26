using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class PosDevice : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Guid BranchId { get; set; }

    public Guid? WarehouseId { get; set; }

    public Guid ServerId { get; set; }

    public string DeviceCode { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public int TerminalNumber { get; set; }

    public string? MacAddress { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public Company Company { get; set; } = null!;

    public Branch Branch { get; set; } = null!;

    public Warehouse? Warehouse { get; set; }

    public BranchServer Server { get; set; } = null!;
}