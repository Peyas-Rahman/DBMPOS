using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class UserRole : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public Guid? BranchId { get; set; }

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public Branch? Branch { get; set; }
}