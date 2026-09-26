using DBM.POS.Domain.Common;

namespace DBM.POS.Domain.Entities;

public class User : BaseEntity
{
    public Guid CompanyId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string PasswordHash { get; set; } = null!;

    public DateTime? LastLoginAt { get; set; }

    public Company Company { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();

    public ICollection<UserBranch> UserBranches { get; set; }
        = new List<UserBranch>();
}