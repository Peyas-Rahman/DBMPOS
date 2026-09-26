using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Unit : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string UnitCode { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public decimal ConversionFactor { get; set; } = 1;
    public Company Company { get; set; } = null!;
}