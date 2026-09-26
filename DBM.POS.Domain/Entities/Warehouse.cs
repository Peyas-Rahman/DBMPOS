using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class Warehouse : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Guid BranchId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Company Company { get; set; } = null!;

    public Branch Branch { get; set; } = null!;
}