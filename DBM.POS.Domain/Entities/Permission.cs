using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class Permission : BaseEntity
{
    public string PermissionCode { get; set; } = null!;

    public string PermissionName { get; set; } = null!;

    public string Module { get; set; } = null!;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}