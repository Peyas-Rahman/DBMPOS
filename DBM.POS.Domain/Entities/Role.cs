using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class Role : BaseEntity
{
    public Guid? CompanyId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public Company? Company { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}