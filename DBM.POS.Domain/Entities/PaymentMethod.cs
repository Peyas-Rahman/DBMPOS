using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class PaymentMethod : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsCash { get; set; }
    public bool IsDefault { get; set; }
}
