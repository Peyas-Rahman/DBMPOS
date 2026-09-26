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

    [HttpGet("categories")] public async Task<IActionResult> Categories()=>Ok(await _db.Categories.Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.CategoryName).ToListAsync());
    [HttpPost("categories")] public async Task<IActionResult> Category(CategoryRequest r)=>await Add(_db.Categories,new Category{CompanyId=CompanyId,CategoryCode=r.Code,CategoryName=r.Name,Description=r.Description},x=>x.CategoryCode==r.Code);
    [HttpGet("brands")] public async Task<IActionResult> Brands()=>Ok(await _db.Brands.Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.BrandName).ToListAsync());
    [HttpPost("brands")] public async Task<IActionResult> Brand(CategoryRequest r)=>await Add(_db.Brands,new Brand{CompanyId=CompanyId,BrandCode=r.Code,BrandName=r.Name},x=>x.BrandCode==r.Code);
    [HttpGet("units")] public async Task<IActionResult> Units()=>Ok(await _db.Units.Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.UnitName).ToListAsync());
    [HttpPost("units")] public async Task<IActionResult> Unit(CategoryRequest r)=>await Add(_db.Units,new Unit{CompanyId=CompanyId,UnitCode=r.Code,UnitName=r.Name,ConversionFactor=r.ConversionFactor<=0?1:r.ConversionFactor},x=>x.UnitCode==r.Code);
    [HttpGet("customers")] public async Task<IActionResult> Customers([FromQuery]string? search)=>Ok(await _db.Customers.Where(x=>x.CompanyId==CompanyId&&x.IsActive&&(string.IsNullOrWhiteSpace(search)||x.CustomerName.Contains(search)||x.Phone!.Contains(search))).OrderBy(x=>x.CustomerName).ToListAsync());
    [HttpPost("customers")] public async Task<IActionResult> Customer(PartyRequest r)=>await Add(_db.Customers,new Customer{CompanyId=CompanyId,CustomerCode=r.Code,CustomerName=r.Name,Phone=r.Phone,Email=r.Email,Address=r.Address,CreditLimit=r.CreditLimit,OpeningDue=r.OpeningDue},x=>x.CustomerCode==r.Code);
    [HttpGet("suppliers")] public async Task<IActionResult> Suppliers([FromQuery]string? search)=>Ok(await _db.Suppliers.Where(x=>x.CompanyId==CompanyId&&x.IsActive&&(string.IsNullOrWhiteSpace(search)||x.SupplierName.Contains(search)||x.Phone!.Contains(search))).OrderBy(x=>x.SupplierName).ToListAsync());
    [HttpPost("suppliers")] public async Task<IActionResult> Supplier(PartyRequest r)=>await Add(_db.Suppliers,new Supplier{CompanyId=CompanyId,SupplierCode=r.Code,SupplierName=r.Name,Phone=r.Phone,Email=r.Email,Address=r.Address,OpeningDue=r.OpeningDue},x=>x.SupplierCode==r.Code);

    private async Task<IActionResult> Add<T>(DbSet<T> set,T entity,Expression<Func<T,bool>> exists) where T:class
    {
        if(await set.AnyAsync(exists))return Conflict(new{message="Code already exists."});
        set.Add(entity);await _db.SaveChangesAsync();return Ok(entity);
    }
    public record CategoryRequest(string Code,string Name,string? Description=null,decimal ConversionFactor=1);
    public record PartyRequest(string Code,string Name,string? Phone=null,string? Email=null,string? Address=null,decimal CreditLimit=0,decimal OpeningDue=0);
}