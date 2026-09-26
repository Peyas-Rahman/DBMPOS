using DBM.POS.API.Services;
using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/inventory")]
public class InventoryController:ControllerBase
{
    private readonly POSDbContext _db; private readonly StockService _stock;
    public InventoryController(POSDbContext db,StockService stock){_db=db;_stock=stock;}
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] Guid? warehouseId, [FromQuery] string? search)
    {
        if (warehouseId.HasValue && !await _db.Warehouses.AnyAsync(x =>
            x.Id == warehouseId.Value && x.CompanyId == CompanyId))
            return BadRequest(new { message = "Invalid warehouse." });

        var q = _db.StockBalances.AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Warehouse)
            .Where(x => x.CompanyId == CompanyId && x.Product.IsActive);

        if (warehouseId.HasValue)
            q = q.Where(x => x.WarehouseId == warehouseId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x =>
                x.Product.ProductName.Contains(search) ||
                x.Product.ProductCode.Contains(search) ||
                (x.Product.Barcode != null && x.Product.Barcode.Contains(search)));

        return Ok(await q
            .OrderBy(x => x.Warehouse.WarehouseName)
            .ThenBy(x => x.Product.ProductName)
            .Select(x => new
            {
                x.Id,
                x.WarehouseId,
                Warehouse = x.Warehouse.WarehouseName,
                x.ProductId,
                x.VariantId,
                x.Quantity,
                x.AverageCost,
                Product = x.Product.ProductName,
                Code = x.Product.ProductCode,
                Barcode = x.Product.Barcode
            })
            .ToListAsync());
    }
    [HttpGet("ledger")] public async Task<IActionResult> Ledger([FromQuery]Guid warehouseId,[FromQuery]Guid? productId)
    {
        var q=_db.StockLedgers.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.WarehouseId==warehouseId);
        if(productId.HasValue)q=q.Where(x=>x.ProductId==productId);
        return Ok(await q.OrderByDescending(x=>x.TransactionDate).Take(1000).ToListAsync());
    }
    [HttpPost("adjust")] public async Task<IActionResult> Adjust(AdjustmentRequest r)
    {
        if (r.Quantity == 0 || r.UnitCost < 0 || string.IsNullOrWhiteSpace(r.Reason))
            return BadRequest(new { message = "Quantity must be non-zero, unit cost cannot be negative, and reason is required." });
        if (!await _db.Warehouses.AnyAsync(x => x.Id == r.WarehouseId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(new { message = "Invalid warehouse." });
        if (!await _db.Products.AnyAsync(x => x.Id == r.ProductId && x.CompanyId == CompanyId && x.IsActive))
            return NotFound(new { message = "Product not found." });
        if (r.VariantId.HasValue && !await _db.ProductVariants.AnyAsync(x =>
            x.Id == r.VariantId.Value && x.ProductId == r.ProductId && x.CompanyId == CompanyId && x.IsActive))
            return BadRequest(new { message = "Invalid product variant." });

        await using var tx=await _db.Database.BeginTransactionAsync();
        try{
            var b=await _stock.ChangeAsync(CompanyId,r.WarehouseId,r.ProductId,r.VariantId,r.Quantity,r.UnitCost,"Adjustment",Guid.NewGuid(),r.Reason);
            await _db.SaveChangesAsync();await tx.CommitAsync();return Ok(b);
        }catch(Exception ex){await tx.RollbackAsync();return BadRequest(new{message=ex.Message});}
    }
    public record AdjustmentRequest(Guid WarehouseId,Guid ProductId,Guid? VariantId,decimal Quantity,decimal UnitCost,string Reason);
}