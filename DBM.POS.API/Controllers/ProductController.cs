using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

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
            Variants = x.Variants.Where(v => v.IsActive).Select(v => new { v.Id, v.VariantCode, v.VariantName, v.Barcode, v.SKU, v.CostPrice, v.SalePrice, v.MRP, Attributes = v.AttributeValues.Where(a=>a.IsActive).Select(a=>new{a.ProductAttributeId,a.Value,AttributeName=a.ProductAttribute.AttributeName}).ToList() }).ToList()
        }).ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var p = await _db.Products.AsNoTracking().Include(x => x.Variants).ThenInclude(x=>x.AttributeValues).ThenInclude(x=>x.ProductAttribute).Include(x => x.Category).Include(x => x.Brand).Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == CompanyId);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ProductName))
            return BadRequest(new { message = "ProductName is required." });
        var productCode=string.IsNullOrWhiteSpace(r.ProductCode)?await GenerateProductCode(r.ProductName):r.ProductCode.Trim();
        if (await _db.Products.AnyAsync(x => x.CompanyId == CompanyId && x.ProductCode == productCode))
            return Conflict(new { message = "Product code already exists." });
        var p = new Product {
            CompanyId=CompanyId, ProductCode=productCode, ProductName=r.ProductName.Trim(), Barcode=r.Barcode,
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
        var productCode=string.IsNullOrWhiteSpace(r.ProductCode)?await GenerateProductCode(r.ProductName,id):r.ProductCode.Trim();
        if(await _db.Products.AnyAsync(x=>x.CompanyId==CompanyId&&x.ProductCode==productCode&&x.Id!=id))
            return Conflict(new {message="Product code already exists."});
        p.ProductCode=productCode; p.ProductName=r.ProductName.Trim(); p.Barcode=r.Barcode; p.Description=r.Description;
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
        var v=await _db.ProductVariants.AsNoTracking().Include(x=>x.Product).Include(x=>x.AttributeValues).ThenInclude(x=>x.ProductAttribute).FirstOrDefaultAsync(x=>x.CompanyId==CompanyId&&x.IsActive&&(x.Barcode==barcode||x.SKU==barcode));
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
        var requestedAttributes=r.Attributes??[];
        var attributeIds=requestedAttributes.Select(x=>x.ProductAttributeId).Distinct().ToList();
        if(attributeIds.Count!=requestedAttributes.Count)return BadRequest(new{message="An attribute can only appear once per variant."});
        var attributes=await _db.ProductAttributes.Where(x=>x.CompanyId==CompanyId&&x.IsActive&&attributeIds.Contains(x.Id)).ToListAsync();
        if(attributes.Count!=attributeIds.Count)return BadRequest(new{message="One or more product attributes are invalid."});
        var v=new ProductVariant{CompanyId=CompanyId,ProductId=id,VariantCode=r.VariantCode,VariantName=r.VariantName,Barcode=r.Barcode,SKU=r.SKU,CostPrice=r.CostPrice,SalePrice=r.SalePrice,MRP=r.MRP};
        foreach(var value in requestedAttributes){if(string.IsNullOrWhiteSpace(value.Value))return BadRequest(new{message="Attribute values cannot be empty."});v.AttributeValues.Add(new ProductAttributeValue{CompanyId=CompanyId,ProductAttributeId=value.ProductAttributeId,Value=value.Value.Trim()});}
        _db.ProductVariants.Add(v);await _db.SaveChangesAsync();return Ok(v);
    }

    public record ProductRequest(string? ProductCode,string ProductName,string? Barcode,string? Description,Guid? CategoryId,Guid? BrandId,Guid? UnitId,decimal CostPrice,decimal SalePrice,decimal MRP,decimal TaxPercent,decimal MinStockLevel,bool TrackBatch,bool TrackExpiry,bool AllowNegativeStock);
    public record VariantAttributeRequest(Guid ProductAttributeId,string Value);
    public record VariantRequest(string VariantCode,string VariantName,string? Barcode,string? SKU,decimal CostPrice,decimal SalePrice,decimal MRP,List<VariantAttributeRequest>? Attributes=null);

    private async Task<string> GenerateProductCode(string productName,Guid? excludeId=null)
    {
        var prefix=Regex.Replace(productName.Trim().ToUpperInvariant(),"[^A-Z0-9]+","-").Trim('-');
        prefix=prefix[..Math.Min(prefix.Length,12)].TrimEnd('-');
        if(string.IsNullOrWhiteSpace(prefix))prefix="ITEM";
        var existing=await _db.Products.Where(x=>x.CompanyId==CompanyId&&x.Id!=excludeId&&x.ProductCode.StartsWith(prefix+"-")).Select(x=>x.ProductCode).ToListAsync();
        var used=existing.Select(code=>int.TryParse(code[(code.LastIndexOf('-')+1)..],out var suffix)?suffix:0).ToHashSet();
        var next=1;while(used.Contains(next))next++;
        return $"{prefix}-{next:000}";
    }
}