using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;

[ApiController, Authorize]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly POSDbContext _db;
    public ProductController(POSDbContext db) => _db = db;
    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] bool activeOnly = true)
    {
        var q = _db.Products.AsNoTracking().Where(x => x.CompanyId == CompanyId);
        if (activeOnly) q = q.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.ProductName.Contains(search) || x.ProductCode.Contains(search) || (x.Barcode != null && x.Barcode.Contains(search)));
        var data = await q.OrderBy(x => x.ProductName).Select(x => new {
            x.Id, x.ProductCode, x.ProductName, x.Barcode, x.CostPrice, x.SalePrice, x.MRP,
            x.TaxPercent, x.MinStockLevel, x.TrackBatch, x.TrackExpiry, x.IsActive,
            Category = x.Category == null ? null : x.Category.CategoryName,
            Brand = x.Brand == null ? null : x.Brand.BrandName,
            Unit = x.Unit == null ? null : x.Unit.UnitName,
            Variants = x.Variants.Where(v => v.IsActive).Select(v => new { v.Id, v.VariantCode, v.VariantName, v.Barcode, v.SKU, v.CostPrice, v.SalePrice, v.MRP }).ToList()
        }).ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Variants).Include(x => x.Category).Include(x => x.Brand).Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ProductCode) || string.IsNullOrWhiteSpace(r.ProductName))
            return BadRequest(new { message = "ProductCode and ProductName are required." });
        if (await _db.Products.AnyAsync(x => x.CompanyId == CompanyId && x.ProductCode == r.ProductCode))
            return Conflict(new { message = "Product code already exists." });
        var p = new Product {
            CompanyId=CompanyId, ProductCode=r.ProductCode.Trim(), ProductName=r.ProductName.Trim(), Barcode=r.Barcode,
            Description=r.Description, CategoryId=r.CategoryId, BrandId=r.BrandId, UnitId=r.UnitId,
            CostPrice=r.CostPrice, SalePrice=r.SalePrice, MRP=r.MRP, TaxPercent=r.TaxPercent,
            MinStockLevel=r.MinStockLevel, TrackBatch=r.TrackBatch, TrackExpiry=r.TrackExpiry, AllowNegativeStock=r.AllowNegativeStock
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id=p.Id }, p);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProductRequest r)
    {
        var p=await _db.Products.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(p==null)return NotFound();
        if(await _db.Products.AnyAsync(x=>x.CompanyId==CompanyId&&x.ProductCode==r.ProductCode&&x.Id!=id))
            return Conflict(new {message="Product code already exists."});
        p.ProductCode=r.ProductCode.Trim(); p.ProductName=r.ProductName.Trim(); p.Barcode=r.Barcode; p.Description=r.Description;
        p.CategoryId=r.CategoryId;p.BrandId=r.BrandId;p.UnitId=r.UnitId;p.CostPrice=r.CostPrice;p.SalePrice=r.SalePrice;p.MRP=r.MRP;
        p.TaxPercent=r.TaxPercent;p.MinStockLevel=r.MinStockLevel;p.TrackBatch=r.TrackBatch;p.TrackExpiry=r.TrackExpiry;p.AllowNegativeStock=r.AllowNegativeStock;
        p.UpdatedAt=DateTime.UtcNow; await _db.SaveChangesAsync(); return Ok(p);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p=await _db.Products.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(p==null)return NotFound();
        p.IsActive=false;p.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return NoContent();
    }


    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> ByBarcode(string barcode)
    {
        var p=await _db.Products.AsNoTracking().Include(x=>x.Variants).FirstOrDefaultAsync(x=>x.CompanyId==CompanyId&&x.IsActive&&x.Barcode==barcode);
        if(p!=null)return Ok(new{type="Product",product=p});
        var v=await _db.ProductVariants.AsNoTracking().Include(x=>x.Product).FirstOrDefaultAsync(x=>x.CompanyId==CompanyId&&x.IsActive&&(x.Barcode==barcode||x.SKU==barcode));
        return v==null?NotFound(new{message="Barcode not found."}):Ok(new{type="Variant",variant=v});
    }

    [HttpPut("variants/{variantId:guid}")]
    public async Task<IActionResult> UpdateVariant(Guid variantId, VariantRequest r)
    {
        var v=await _db.ProductVariants.FirstOrDefaultAsync(x=>x.Id==variantId&&x.CompanyId==CompanyId);
        if(v==null)return NotFound();
        if(await _db.ProductVariants.AnyAsync(x=>x.CompanyId==CompanyId&&x.VariantCode==r.VariantCode&&x.Id!=variantId))return Conflict(new{message="Variant code already exists."});
        v.VariantCode=r.VariantCode;v.VariantName=r.VariantName;v.Barcode=r.Barcode;v.SKU=r.SKU;v.CostPrice=r.CostPrice;v.SalePrice=r.SalePrice;v.MRP=r.MRP;v.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok(v);
    }
    [HttpDelete("variants/{variantId:guid}")]
    public async Task<IActionResult> DeleteVariant(Guid variantId){var v=await _db.ProductVariants.FirstOrDefaultAsync(x=>x.Id==variantId&&x.CompanyId==CompanyId);if(v==null)return NotFound();v.IsActive=false;v.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return NoContent();}

    [HttpPost("{id:guid}/variants")]
    public async Task<IActionResult> AddVariant(Guid id, VariantRequest r)
    {
        var p=await _db.Products.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(p==null)return NotFound();
        if(await _db.ProductVariants.AnyAsync(x=>x.CompanyId==CompanyId&&x.VariantCode==r.VariantCode))
            return Conflict(new {message="Variant code already exists."});
        var v=new ProductVariant{CompanyId=CompanyId,ProductId=id,VariantCode=r.VariantCode,VariantName=r.VariantName,Barcode=r.Barcode,SKU=r.SKU,CostPrice=r.CostPrice,SalePrice=r.SalePrice,MRP=r.MRP};
        _db.ProductVariants.Add(v);await _db.SaveChangesAsync();return Ok(v);
    }

    public record ProductRequest(string ProductCode,string ProductName,string? Barcode,string? Description,Guid? CategoryId,Guid? BrandId,Guid? UnitId,decimal CostPrice,decimal SalePrice,decimal MRP,decimal TaxPercent,decimal MinStockLevel,bool TrackBatch,bool TrackExpiry,bool AllowNegativeStock);
    public record VariantRequest(string VariantCode,string VariantName,string? Barcode,string? SKU,decimal CostPrice,decimal SalePrice,decimal MRP);
}