using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class StockTransfer : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid FromBranchId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToBranchId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public string TransferNo { get; set; } = null!;
    public string Status { get; set; } = "Requested";
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}