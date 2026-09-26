using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class UserBranch : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid BranchId { get; set; }

    public User User { get; set; } = null!;

    public Branch Branch { get; set; } = null!;
}