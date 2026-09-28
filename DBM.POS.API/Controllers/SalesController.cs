using DBM.POS.API.Services;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/sales")]
public class SalesController:ControllerBase
{
    private readonly POSDbContext _db; private readonly StockService _stock; private readonly LedgerService _ledger;
    public SalesController(POSDbContext db,StockService stock,LedgerService ledger){_db=db;_stock=stock;_ledger=ledger;}
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery]DateTime? from,[FromQuery]DateTime? to)
    {
        var q=_db.Sales.AsNoTracking().Where(x=>x.CompanyId==CompanyId);
        if(from.HasValue)q=q.Where(x=>x.SaleDate>=from);
        if(to.HasValue)q=q.Where(x=>x.SaleDate<to.Value.Date.AddDays(1));
        return Ok(await q.OrderByDescending(x=>x.SaleDate).Select(x=>new{x.Id,x.InvoiceNo,x.SaleDate,x.CustomerId,CustomerName=x.Customer!=null?x.Customer.CustomerName:null,CustomerPhone=x.Customer!=null?x.Customer.Phone:null,SoldBy=_db.Users.Where(u=>u.Id==x.UserId).Select(u=>u.FullName).FirstOrDefault(),x.SubTotal,x.Discount,x.Tax,x.GrandTotal,x.PaidAmount,x.DueAmount,x.PaymentStatus}).ToListAsync());
    }
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id)
    {
        var s=await _db.Sales.AsNoTracking().Include(x=>x.Customer).Include(x=>x.User).Include(x=>x.Items).ThenInclude(x=>x.Product).Include(x=>x.Items).ThenInclude(x=>x.Variant).Include(x=>x.Payments).FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        return s==null?NotFound():Ok(s);
    }
    [HttpPost] public async Task<IActionResult> Create(SaleRequest r)
    {
        if(r.Items.Count==0)return BadRequest(new{message="At least one item is required."});
        if(!await _db.Branches.AnyAsync(x=>x.Id==r.BranchId&&x.CompanyId==CompanyId&&x.IsActive))
            return BadRequest(new{message="Invalid branch."});
        if(!await _db.Warehouses.AnyAsync(x=>x.Id==r.WarehouseId&&x.CompanyId==CompanyId&&x.BranchId==r.BranchId&&x.IsActive))
            return BadRequest(new{message="Invalid warehouse for the selected branch."});
        if(r.PaidAmount<0) return BadRequest(new{message="Paid amount cannot be negative."});
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            var no=string.IsNullOrWhiteSpace(r.InvoiceNo)?$"INV-{DateTime.UtcNow:yyyyMMddHHmmssfff}":r.InvoiceNo;
            if(await _db.Sales.AnyAsync(x=>x.CompanyId==CompanyId&&x.InvoiceNo==no))return Conflict(new{message="Invoice number exists."});
            var s=new Sale{CompanyId=CompanyId,BranchId=r.BranchId,WarehouseId=r.WarehouseId,CustomerId=r.CustomerId,PosDeviceId=r.PosDeviceId,UserId=Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var uid)?uid:(Guid?)null,InvoiceNo=no};
            foreach(var i in r.Items){
                if(i.Quantity<=0) throw new InvalidOperationException("Quantity must be greater than zero.");
                if(i.UnitPrice<0 || i.UnitCost<0) throw new InvalidOperationException("Price and cost cannot be negative.");
                if(i.Discount<0 || i.Tax<0) throw new InvalidOperationException("Discount and tax cannot be negative.");
                var product=await _db.Products.FirstOrDefaultAsync(x=>x.Id==i.ProductId&&x.CompanyId==CompanyId&&x.IsActive);
                if(product==null)throw new InvalidOperationException("Invalid product.");
                var hasVariants=await _db.ProductVariants.AnyAsync(x=>x.CompanyId==CompanyId&&x.ProductId==i.ProductId&&x.IsActive);
                if(hasVariants&&!i.VariantId.HasValue)throw new InvalidOperationException("Select a variant for this product.");
                if(i.VariantId.HasValue&&!await _db.ProductVariants.AnyAsync(x=>x.Id==i.VariantId.Value&&x.CompanyId==CompanyId&&x.ProductId==i.ProductId&&x.IsActive))
                    throw new InvalidOperationException("Invalid product variant.");
                var cost=i.UnitCost>0?i.UnitCost:product.CostPrice;
                var line=(i.Quantity*i.UnitPrice)-i.Discount+i.Tax;
                s.Items.Add(new SaleItem{ProductId=i.ProductId,VariantId=i.VariantId,Quantity=i.Quantity,UnitPrice=i.UnitPrice,UnitCost=cost,Discount=i.Discount,Tax=i.Tax,LineTotal=line});
            }
            s.SubTotal=s.Items.Sum(x=>x.Quantity*x.UnitPrice);s.Discount=s.Items.Sum(x=>x.Discount);s.Tax=s.Items.Sum(x=>x.Tax);s.GrandTotal=s.SubTotal-s.Discount+s.Tax;
            if(r.CustomerId.HasValue)
            {
                var customer=await _db.Customers.FirstOrDefaultAsync(x=>x.Id==r.CustomerId&&x.CompanyId==CompanyId&&x.IsActive);
                if(customer==null) throw new InvalidOperationException("Invalid customer.");
                var existingDue=await _ledger.CustomerBalance(CompanyId,customer.Id);
                var newDue=s.GrandTotal-r.PaidAmount;
                if(customer.CreditLimit>0 && existingDue+newDue>customer.CreditLimit) throw new InvalidOperationException($"Customer credit limit exceeded. Available: {Math.Max(0,customer.CreditLimit-existingDue)}");
            }
            if(r.PaidAmount>s.GrandTotal) throw new InvalidOperationException("Paid amount cannot exceed grand total.");
            if(r.Payments!=null && r.Payments.Count>0)
            {
                if(r.Payments.Any(x=>x.Amount<=0)) throw new InvalidOperationException("Payment amount must be greater than zero.");
                var paymentTotal=r.Payments.Sum(x=>x.Amount);
                if(paymentTotal!=r.PaidAmount) throw new InvalidOperationException("Payment total must equal paid amount.");
            }
            s.PaidAmount=r.PaidAmount;s.DueAmount=s.GrandTotal-s.PaidAmount;s.PaymentStatus=s.DueAmount<=0?"Paid":s.PaidAmount>0?"Partial":"Due";
            _db.Sales.Add(s);await _db.SaveChangesAsync();
            if(s.CustomerId.HasValue && s.DueAmount>0) await _ledger.AddCustomer(CompanyId,s.CustomerId.Value,s.BranchId,"Sale",s.Id,s.InvoiceNo,s.DueAmount,0,"Credit sale",s.UserId);
            foreach(var i in s.Items)await _stock.ChangeAsync(CompanyId,s.WarehouseId,i.ProductId,i.VariantId,-i.Quantity,i.UnitCost,"Sale",s.Id,s.InvoiceNo);
            if(r.Payments!=null)foreach(var pm in r.Payments)_db.Payments.Add(new Payment{CompanyId=CompanyId,SaleId=s.Id,PaymentMethod=pm.Method,Amount=pm.Amount,ReferenceNo=pm.ReferenceNo});
            await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(s);
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }
    public record SaleRequest(Guid BranchId,Guid WarehouseId,Guid? CustomerId,Guid? PosDeviceId,string? InvoiceNo,decimal PaidAmount,List<Item> Items,List<PaymentItem>? Payments=null);
    public record Item(Guid ProductId,Guid? VariantId,decimal Quantity,decimal UnitPrice,decimal UnitCost=0,decimal Discount=0,decimal Tax=0);
    public record PaymentItem(string Method,decimal Amount,string? ReferenceNo=null);
}