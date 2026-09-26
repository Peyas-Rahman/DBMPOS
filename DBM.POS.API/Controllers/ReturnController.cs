using DBM.POS.API.Services;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/returns")]
public class ReturnController:ControllerBase
{
    private readonly POSDbContext _db; private readonly StockService _stock; private readonly LedgerService _ledger;
    public ReturnController(POSDbContext db,StockService stock,LedgerService ledger){_db=db;_stock=stock;_ledger=ledger;}
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);
    private Guid? UserId=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:(Guid?)null;

    [HttpGet("sale/{saleId:guid}/history")]
    public async Task<IActionResult> SaleReturnHistory(Guid saleId)
    {
        return Ok(await _db.StockLedgers.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.ReferenceId==saleId&&x.TransactionType=="SalesReturn").OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.ProductId,x.VariantId,x.QuantityIn,x.UnitCost,x.ReferenceNo,x.CreatedAt}).ToListAsync());
    }

    [HttpPost("sale/{saleId:guid}")]
    public async Task<IActionResult> SaleReturn(Guid saleId, ReturnRequest r)
    {
        var s=await _db.Sales.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==saleId&&x.CompanyId==CompanyId);
        if(s==null)return NotFound();
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            decimal returnTotal=0;
            foreach(var i in r.Items){
                var sold=s.Items.FirstOrDefault(x=>x.ProductId==i.ProductId&&x.VariantId==i.VariantId);
                if(sold==null||i.Quantity<=0||i.Quantity>sold.Quantity)throw new InvalidOperationException("Invalid return quantity.");
                var alreadyReturned=await _db.StockLedgers.Where(x=>x.CompanyId==CompanyId&&x.ReferenceId==s.Id&&x.TransactionType=="SalesReturn"&&x.ProductId==i.ProductId&&x.VariantId==i.VariantId).SumAsync(x=>(decimal?)x.QuantityIn)??0;
                if(alreadyReturned+i.Quantity>sold.Quantity)throw new InvalidOperationException($"Return quantity exceeds remaining quantity for {sold.ProductId}.");
                returnTotal+=i.Quantity*(sold.UnitPrice-(sold.Discount/sold.Quantity)+(sold.Tax/sold.Quantity));
                await _stock.ChangeAsync(CompanyId,s.WarehouseId,i.ProductId,i.VariantId,i.Quantity,sold.UnitCost,"SalesReturn",s.Id,s.InvoiceNo);
            }
            decimal customerCredit=0; decimal currentDue=0;
            if(s.CustomerId.HasValue){var balance=await _ledger.CustomerBalance(CompanyId,s.CustomerId.Value);customerCredit=Math.Min(returnTotal,Math.Max(0,balance));if(customerCredit>0)await _ledger.AddCustomer(CompanyId,s.CustomerId.Value,s.BranchId,"SalesReturn",s.Id,s.InvoiceNo,0,customerCredit,"Sales return credit",UserId);currentDue=balance-customerCredit;}
            var cashRefund=returnTotal-customerCredit;
            if(cashRefund>0)_db.CashTransactions.Add(new CashTransaction{CompanyId=CompanyId,BranchId=s.BranchId,TransactionType="SalesReturn",Amount=cashRefund,PaymentMethod="Cash",ReferenceNo=s.InvoiceNo,Description="Cash refund for sales return",UserId=UserId});
            await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Sales return completed",saleId,returnTotal,customerCredit,cashRefund,currentDue});
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }

    [HttpPost("purchase/{purchaseId:guid}")]
    public async Task<IActionResult> PurchaseReturn(Guid purchaseId, ReturnRequest r)
    {
        var p=await _db.Purchases.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==purchaseId&&x.CompanyId==CompanyId);
        if(p==null)return NotFound();
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            foreach(var i in r.Items){
                var bought=p.Items.FirstOrDefault(x=>x.ProductId==i.ProductId&&x.VariantId==i.VariantId);
                if(bought==null||i.Quantity<=0||i.Quantity>bought.Quantity)throw new InvalidOperationException("Invalid return quantity.");
                await _stock.ChangeAsync(CompanyId,p.WarehouseId,i.ProductId,i.VariantId,-i.Quantity,bought.UnitCost,"PurchaseReturn",p.Id,p.PurchaseNo);
            }
            await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(new{message="Purchase return completed",purchaseId});
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }
    public record ReturnRequest(List<Item> Items,string? Notes=null);
    public record Item(Guid ProductId,Guid? VariantId,decimal Quantity);
}