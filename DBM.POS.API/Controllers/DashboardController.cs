using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/dashboard")]
public class DashboardController:ControllerBase
{
    private readonly POSDbContext _db;
    public DashboardController(POSDbContext db)=>_db=db;
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var today=DateTime.UtcNow.Date;
        var tomorrow=today.AddDays(1);
        var sales=await _db.Sales.Where(x=>x.CompanyId==CompanyId&&x.SaleDate>=today&&x.SaleDate<tomorrow).ToListAsync();
        var purchases=await _db.Purchases.Where(x=>x.CompanyId==CompanyId&&x.PurchaseDate>=today&&x.PurchaseDate<tomorrow).ToListAsync();
        var cashRefunds=await _db.CashTransactions.Where(x=>x.CompanyId==CompanyId&&x.TransactionType=="SalesReturn"&&x.TransactionDate>=today&&x.TransactionDate<tomorrow).SumAsync(x=>(decimal?)x.Amount)??0;
        var stock=await _db.StockBalances.Where(x=>x.CompanyId==CompanyId).ToListAsync();
        var returns=await _db.StockLedgers.Where(x=>x.CompanyId==CompanyId&&x.TransactionType=="SalesReturn"&&x.TransactionDate>=today&&x.TransactionDate<tomorrow).ToListAsync();
        var returnSaleIds=returns.Select(x=>x.ReferenceId).Distinct().ToList();
        var returnedSaleItems=await _db.SaleItems.AsNoTracking().Where(x=>returnSaleIds.Contains(x.SaleId)).ToListAsync();
        decimal salesReturnAmount=0,salesReturnProfit=0;
        foreach(var returned in returns){var sold=returnedSaleItems.FirstOrDefault(x=>x.SaleId==returned.ReferenceId&&x.ProductId==returned.ProductId&&x.VariantId==returned.VariantId);if(sold==null)continue;var revenuePerUnit=sold.UnitPrice-(sold.Quantity==0?0:sold.Discount/sold.Quantity)+(sold.Quantity==0?0:sold.Tax/sold.Quantity);salesReturnAmount+=returned.QuantityIn*revenuePerUnit;salesReturnProfit+=returned.QuantityIn*(revenuePerUnit-sold.UnitCost);}
        var customerBalances=await _db.Customers.Where(x=>x.CompanyId==CompanyId&&x.IsActive).Select(x=>_db.CustomerLedgers.Where(l=>l.CompanyId==CompanyId&&l.CustomerId==x.Id).OrderByDescending(l=>l.TransactionDate).ThenByDescending(l=>l.CreatedAt).Select(l=>(decimal?)l.Balance).FirstOrDefault()??x.OpeningDue).ToListAsync();
        return Ok(new{
            date=today,
            salesCount=sales.Count,
            salesAmount=sales.Sum(x=>x.GrandTotal),
            paidAmount=sales.Sum(x=>x.PaidAmount)-cashRefunds,
            cashRefunds,
            dueAmount=customerBalances.Where(x=>x>0).Sum(),
            salesReturnCount=returns.Select(x=>x.ReferenceId).Distinct().Count(),
            salesReturnAmount,
            salesReturnProfit,
            purchaseCount=purchases.Count,
            purchaseAmount=purchases.Sum(x=>x.GrandTotal),
            totalStockUnits=stock.Sum(x=>x.Quantity),
            products=await _db.Products.CountAsync(x=>x.CompanyId==CompanyId&&x.IsActive),
            customers=await _db.Customers.CountAsync(x=>x.CompanyId==CompanyId&&x.IsActive),
            suppliers=await _db.Suppliers.CountAsync(x=>x.CompanyId==CompanyId&&x.IsActive)
        });
    }
}