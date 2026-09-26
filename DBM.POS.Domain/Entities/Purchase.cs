using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Purchase : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid SupplierId { get; set; }
    public string PurchaseNo { get; set; } = null!;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}