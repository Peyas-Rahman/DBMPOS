using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;

[ApiController, Authorize, Route("api/master")]
public class MasterController : ControllerBase
{
    private readonly POSDbContext _db;
    public MasterController(POSDbContext db)=>_db=db;
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("categories")] public async Task<IActionResult> Categories()=>Ok(await _db.Categories.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.CategoryName).Select(x=>new{x.Id,x.CategoryCode,x.CategoryName,x.Description,x.ParentCategoryId,ParentCategoryName=x.ParentCategory==null?null:x.ParentCategory.CategoryName}).ToListAsync());
    [HttpPost("categories")] public async Task<IActionResult> Category(CategoryRequest r)
    {
        var code=r.Code.Trim();var name=r.Name.Trim();
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name))return BadRequest(new{message="Category code and name are required."});
        if(await _db.Categories.AnyAsync(x=>x.CompanyId==CompanyId&&x.CategoryCode==code))return Conflict(new{message="Category code already exists."});
        if(r.ParentCategoryId.HasValue&&!await _db.Categories.AnyAsync(x=>x.Id==r.ParentCategoryId.Value&&x.CompanyId==CompanyId&&x.IsActive&&x.ParentCategoryId==null))
            return BadRequest(new{message="Select an active top-level category as the parent."});
        var category=new Category{CompanyId=CompanyId,CategoryCode=code,CategoryName=name,Description=r.Description,ParentCategoryId=r.ParentCategoryId};
        _db.Categories.Add(category);await _db.SaveChangesAsync();return Ok(category);
    }
    [HttpGet("brands")] public async Task<IActionResult> Brands()=>Ok(await _db.Brands.Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.BrandName).ToListAsync());
    [HttpPost("brands")] public async Task<IActionResult> Brand(CategoryRequest r)=>await Add(_db.Brands,new Brand{CompanyId=CompanyId,BrandCode=r.Code,BrandName=r.Name},x=>x.BrandCode==r.Code);
    [HttpGet("units")] public async Task<IActionResult> Units()=>Ok(await _db.Units.Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.UnitName).ToListAsync());
    [HttpPost("units")] public async Task<IActionResult> Unit(CategoryRequest r)=>await Add(_db.Units,new Unit{CompanyId=CompanyId,UnitCode=r.Code,UnitName=r.Name,ConversionFactor=r.ConversionFactor<=0?1:r.ConversionFactor},x=>x.UnitCode==r.Code);
    [HttpGet("product-attributes")] public async Task<IActionResult> ProductAttributes()=>Ok(await _db.ProductAttributes.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.AttributeName).Select(x=>new{x.Id,x.AttributeCode,x.AttributeName,x.DefaultValues}).ToListAsync());
    [HttpPost("product-attributes")] public async Task<IActionResult> AddProductAttribute(ProductAttributeRequest r)
    {
        var code=r.Code.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(r.Name))return BadRequest(new{message="Attribute code and name are required."});
        if(await _db.ProductAttributes.AnyAsync(x=>x.CompanyId==CompanyId&&x.AttributeCode==code))return Conflict(new{message="Attribute code already exists."});
        var attribute=new ProductAttribute{CompanyId=CompanyId,AttributeCode=code,AttributeName=r.Name.Trim(),DefaultValues=NormalizeValues(r.DefaultValues)};
        _db.ProductAttributes.Add(attribute);await _db.SaveChangesAsync();return Ok(attribute);
    }
    [HttpPut("product-attributes/{id:guid}")] public async Task<IActionResult> UpdateProductAttribute(Guid id,ProductAttributeRequest r)
    {
        var attribute=await _db.ProductAttributes.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);
        if(attribute==null)return NotFound();
        var code=r.Code.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(r.Name))return BadRequest(new{message="Attribute code and name are required."});
        if(await _db.ProductAttributes.AnyAsync(x=>x.CompanyId==CompanyId&&x.AttributeCode==code&&x.Id!=id))return Conflict(new{message="Attribute code already exists."});
        attribute.AttributeCode=code;attribute.AttributeName=r.Name.Trim();attribute.DefaultValues=NormalizeValues(r.DefaultValues);attribute.UpdatedAt=DateTime.UtcNow;
        await _db.SaveChangesAsync();return Ok(attribute);
    }
    [HttpGet("customers")] public async Task<IActionResult> Customers([FromQuery]string? search)=>Ok(await _db.Customers.Where(x=>x.CompanyId==CompanyId&&x.IsActive&&(string.IsNullOrWhiteSpace(search)||x.CustomerName.Contains(search)||x.Phone!.Contains(search))).OrderBy(x=>x.CustomerName).ToListAsync());
    [HttpPost("customers")] public async Task<IActionResult> Customer(PartyRequest r)=>await Add(_db.Customers,new Customer{CompanyId=CompanyId,CustomerCode=r.Code,CustomerName=r.Name,Phone=r.Phone,Email=r.Email,Address=r.Address,CreditLimit=r.CreditLimit,OpeningDue=r.OpeningDue},x=>x.CustomerCode==r.Code);
    [HttpGet("suppliers")] public async Task<IActionResult> Suppliers([FromQuery]string? search)=>Ok(await _db.Suppliers.Where(x=>x.CompanyId==CompanyId&&x.IsActive&&(string.IsNullOrWhiteSpace(search)||x.SupplierName.Contains(search)||x.Phone!.Contains(search))).OrderBy(x=>x.SupplierName).ToListAsync());
    [HttpPost("suppliers")] public async Task<IActionResult> Supplier(PartyRequest r)=>await Add(_db.Suppliers,new Supplier{CompanyId=CompanyId,SupplierCode=r.Code,SupplierName=r.Name,Phone=r.Phone,Email=r.Email,Address=r.Address,OpeningDue=r.OpeningDue},x=>x.SupplierCode==r.Code);

    private async Task<IActionResult> Add<T>(DbSet<T> set,T entity,Expression<Func<T,bool>> exists) where T:class
    {
        if(await set.AnyAsync(exists))return Conflict(new{message="Code already exists."});
        set.Add(entity);await _db.SaveChangesAsync();return Ok(entity);
    }
    public record CategoryRequest(string Code,string Name,string? Description=null,decimal ConversionFactor=1,Guid? ParentCategoryId=null);
    public record ProductAttributeRequest(string Code,string Name,string? DefaultValues=null);
    public record PartyRequest(string Code,string Name,string? Phone=null,string? Email=null,string? Address=null,decimal CreditLimit=0,decimal OpeningDue=0);
    private static string NormalizeValues(string? values)=>string.Join(",",(values??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase));
}