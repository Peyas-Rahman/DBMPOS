using DBM.POS.Domain.Common;
namespace DBM.POS.Domain.Entities;
public class StockTransferItem : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant? Variant { get; set; }
}