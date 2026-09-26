using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class Sale : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PosDeviceId { get; set; }
    public Guid? UserId { get; set; }
    public string InvoiceNo { get; set; } = null!;
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    public Customer? Customer { get; set; }
    public User? User { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}