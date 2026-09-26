using DBM.POS.API.Services;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/purchases")]
public class PurchaseController:ControllerBase
{
    private readonly POSDbContext _db; private readonly StockService _stock; private readonly LedgerService _ledger;
    public PurchaseController(POSDbContext db,StockService stock,LedgerService ledger){_db=db;_stock=stock;_ledger=ledger;}
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery]DateTime? from,[FromQuery]DateTime? to)
    {
        var q=_db.Purchases.AsNoTracking().Where(x=>x.CompanyId==CompanyId);
        if(from.HasValue)q=q.Where(x=>x.PurchaseDate>=from);
        if(to.HasValue)q=q.Where(x=>x.PurchaseDate<to.Value.Date.AddDays(1));
        return Ok(await q.OrderByDescending(x=>x.PurchaseDate).Select(x=>new{x.Id,x.PurchaseNo,x.PurchaseDate,x.SupplierId,x.SubTotal,x.Discount,x.Tax,x.GrandTotal,x.PaidAmount,x.DueAmount}).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Create(PurchaseRequest r)
    {
        if(r.Items.Count==0)return BadRequest(new{message="At least one item is required."});
        if(!await _db.Branches.AnyAsync(x=>x.Id==r.BranchId&&x.CompanyId==CompanyId&&x.IsActive))
            return BadRequest(new{message="Invalid branch."});
        if(!await _db.Warehouses.AnyAsync(x=>x.Id==r.WarehouseId&&x.CompanyId==CompanyId&&x.BranchId==r.BranchId&&x.IsActive))
            return BadRequest(new{message="Invalid warehouse for the selected branch."});
        if(!await _db.Suppliers.AnyAsync(x=>x.Id==r.SupplierId&&x.CompanyId==CompanyId&&x.IsActive))
            return BadRequest(new{message="Invalid supplier."});
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            var no=string.IsNullOrWhiteSpace(r.PurchaseNo)?$"PUR-{DateTime.UtcNow:yyyyMMddHHmmssfff}":r.PurchaseNo;
            if(await _db.Purchases.AnyAsync(x=>x.CompanyId==CompanyId&&x.PurchaseNo==no))return Conflict(new{message="Purchase number exists."});
            var p=new Purchase{CompanyId=CompanyId,BranchId=r.BranchId,WarehouseId=r.WarehouseId,SupplierId=r.SupplierId,PurchaseNo=no,PaidAmount=r.PaidAmount};
            foreach(var i in r.Items){
                if(i.Quantity<=0) throw new InvalidOperationException("Quantity must be greater than zero.");
                if(i.UnitCost<0) throw new InvalidOperationException("Unit cost cannot be negative.");
                if(i.Discount<0 || i.Tax<0) throw new InvalidOperationException("Discount and tax cannot be negative.");
                var product=await _db.Products.FirstOrDefaultAsync(x=>x.Id==i.ProductId&&x.CompanyId==CompanyId&&x.IsActive);
                if(product==null)throw new InvalidOperationException("Invalid product.");
                var line=(i.Quantity*i.UnitCost)-i.Discount+i.Tax;
                p.Items.Add(new PurchaseItem{ProductId=i.ProductId,VariantId=i.VariantId,Quantity=i.Quantity,UnitCost=i.UnitCost,Discount=i.Discount,Tax=i.Tax,LineTotal=line});
            }
            p.SubTotal=p.Items.Sum(x=>x.Quantity*x.UnitCost);p.Discount=p.Items.Sum(x=>x.Discount);p.Tax=p.Items.Sum(x=>x.Tax);p.GrandTotal=p.SubTotal-p.Discount+p.Tax;if(p.PaidAmount>p.GrandTotal)throw new InvalidOperationException("Paid amount cannot exceed purchase total.");p.DueAmount=p.GrandTotal-p.PaidAmount;
            _db.Purchases.Add(p);await _db.SaveChangesAsync();
            if(p.DueAmount>0) await _ledger.AddSupplier(CompanyId,p.SupplierId,p.BranchId,"Purchase",p.Id,p.PurchaseNo,p.DueAmount,0,"Credit purchase",null);
            foreach(var i in p.Items)await _stock.ChangeAsync(CompanyId,p.WarehouseId,i.ProductId,i.VariantId,i.Quantity,i.UnitCost,"Purchase",p.Id,p.PurchaseNo);
            await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(p);
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }
    public record PurchaseRequest(Guid BranchId,Guid WarehouseId,Guid SupplierId,string? PurchaseNo,decimal PaidAmount,List<Item> Items);
    public record Item(Guid ProductId,Guid? VariantId,decimal Quantity,decimal UnitCost,decimal Discount=0,decimal Tax=0);
}