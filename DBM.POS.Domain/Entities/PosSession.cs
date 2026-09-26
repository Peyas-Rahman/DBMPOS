using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class PosSession : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? PosDeviceId { get; set; }
    public Guid OpenedBy { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal CashSales { get; set; }
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal CashRefund { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Difference { get; set; }
    public string Status { get; set; } = "Open";
}
