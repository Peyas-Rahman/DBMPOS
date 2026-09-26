using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DBM.POS.API.Services;

public class StockService
{
    private readonly POSDbContext _db;
    public StockService(POSDbContext db)=>_db=db;

    public async Task<StockBalance> ChangeAsync(Guid companyId, Guid warehouseId, Guid productId, Guid? variantId,
        decimal delta, decimal unitCost, string type, Guid referenceId, string? referenceNo)
    {
        var balance=await _db.StockBalances.FirstOrDefaultAsync(x=>x.CompanyId==companyId&&x.WarehouseId==warehouseId&&x.ProductId==productId&&x.VariantId==variantId);
        if(balance==null)
        {
            balance=new StockBalance{CompanyId=companyId,WarehouseId=warehouseId,ProductId=productId,VariantId=variantId,Quantity=0,AverageCost=unitCost};
            _db.StockBalances.Add(balance);
        }
        if(delta<0)
        {
            var product=await _db.Products.FirstAsync(x=>x.Id==productId);
            if(!product.AllowNegativeStock && balance.Quantity + delta < 0)
                throw new InvalidOperationException($"Insufficient stock for product {product.ProductName}. Available: {balance.Quantity}");
        }
        var oldQty=balance.Quantity;
        var newQty=oldQty+delta;
        if(delta>0)
            balance.AverageCost = newQty<=0 ? unitCost : ((oldQty*balance.AverageCost)+(delta*unitCost))/newQty;
        balance.Quantity=newQty; balance.UpdatedAt=DateTime.UtcNow;
        _db.StockLedgers.Add(new StockLedger{
            CompanyId=companyId,WarehouseId=warehouseId,ProductId=productId,VariantId=variantId,
            TransactionType=type,ReferenceId=referenceId,ReferenceNo=referenceNo,
            QuantityIn=delta>0?delta:0,QuantityOut=delta<0?-delta:0,BalanceAfter=newQty,UnitCost=unitCost
        });
        return balance;
    }
}