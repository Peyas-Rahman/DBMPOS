using DBM.POS.API.Services;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/transfers")]
public class TransferController:ControllerBase
{
    private readonly POSDbContext _db; private readonly StockService _stock;
    public TransferController(POSDbContext db,StockService stock){_db=db;_stock=stock;}
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet] public async Task<IActionResult> Get()=>Ok(await _db.StockTransfers.AsNoTracking().Where(x=>x.CompanyId==CompanyId).OrderByDescending(x=>x.TransferDate).ToListAsync());

    [HttpPost] public async Task<IActionResult> Create(CreateTransferRequest r)
    {
        if(r.FromWarehouseId==r.ToWarehouseId)return BadRequest(new{message="Source and destination warehouse cannot be the same."});
        if(r.Items.Count==0)return BadRequest(new{message="At least one item is required."});
        if(!await _db.Warehouses.AnyAsync(x=>x.Id==r.FromWarehouseId&&x.CompanyId==CompanyId)||!await _db.Warehouses.AnyAsync(x=>x.Id==r.ToWarehouseId&&x.CompanyId==CompanyId))
            return BadRequest(new{message="Invalid warehouse."});
        var t=new StockTransfer{CompanyId=CompanyId,FromBranchId=r.FromBranchId,FromWarehouseId=r.FromWarehouseId,ToBranchId=r.ToBranchId,ToWarehouseId=r.ToWarehouseId,TransferNo=string.IsNullOrWhiteSpace(r.TransferNo)?$"TRF-{DateTime.UtcNow:yyyyMMddHHmmssfff}":r.TransferNo,Status="Requested"};
        t.Items=r.Items.Select(i=>new StockTransferItem{ProductId=i.ProductId,VariantId=i.VariantId,Quantity=i.Quantity}).ToList();
        _db.StockTransfers.Add(t);await _db.SaveChangesAsync();return Ok(t);
    }

    [HttpPost("{id:guid}/dispatch")]
    public async Task<IActionResult> Dispatch(Guid id)
    {
        var t=await _db.StockTransfers.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(t==null)return NotFound(); if(t.Status!="Requested"&&t.Status!="Approved")return BadRequest(new{message="Only Requested/Approved transfers can be dispatched."});
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            foreach(var i in t.Items) {
                var p=await _db.Products.FirstAsync(x=>x.Id==i.ProductId);
                var cost=p.CostPrice;
                await _stock.ChangeAsync(CompanyId,t.FromWarehouseId,i.ProductId,i.VariantId,-i.Quantity,cost,"TransferOut",t.Id,t.TransferNo);
            }
            t.Status="In Transit";t.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(t);
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(Guid id)
    {
        var t=await _db.StockTransfers.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(t==null)return NotFound(); if(t.Status!="In Transit")return BadRequest(new{message="Only In Transit transfers can be received."});
        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            foreach(var i in t.Items) {
                var p=await _db.Products.FirstAsync(x=>x.Id==i.ProductId);
                await _stock.ChangeAsync(CompanyId,t.ToWarehouseId,i.ProductId,i.VariantId,i.Quantity,p.CostPrice,"TransferIn",t.Id,t.TransferNo);
            }
            t.Status="Received";t.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(t);
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }
    public record CreateTransferRequest(Guid FromBranchId,Guid FromWarehouseId,Guid ToBranchId,Guid ToWarehouseId,string? TransferNo,List<Item> Items);
    public record Item(Guid ProductId,Guid? VariantId,decimal Quantity);
}