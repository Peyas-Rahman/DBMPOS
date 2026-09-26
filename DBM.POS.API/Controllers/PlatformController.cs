using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize(Roles="Super Admin"),Route("api/platform")]
public class PlatformController:ControllerBase
{
    private readonly POSDbContext _db;
    public PlatformController(POSDbContext db)=>_db=db;

    [HttpGet("companies")] public async Task<IActionResult> Companies()=>Ok(await _db.Companies.AsNoTracking().OrderBy(x=>x.CompanyName).Select(x=>new{x.Id,x.CompanyCode,x.CompanyName,x.BusinessType,x.Phone,x.Email,x.IsActive,Branches=x.Branches.Count}).ToListAsync());
    [HttpPost("companies")] public async Task<IActionResult> Create(CompanyRequest r)
    {
        if(await _db.Companies.AnyAsync(x=>x.CompanyCode==r.Code))return Conflict(new{message="Company code already exists."});
        var c=new Company{CompanyCode=r.Code,CompanyName=r.Name,BusinessType=r.BusinessType,Phone=r.Phone,Email=r.Email,Address=r.Address,CurrencyCode=string.IsNullOrWhiteSpace(r.CurrencyCode)?"BDT":r.CurrencyCode,TimeZone=string.IsNullOrWhiteSpace(r.TimeZone)?"Asia/Dhaka":r.TimeZone};
        _db.Companies.Add(c);await _db.SaveChangesAsync();
        var b=new Branch{CompanyId=c.Id,BranchCode="HO",BranchName="Head Office",IsHeadOffice=true};
        _db.Branches.Add(b);await _db.SaveChangesAsync();
        _db.Warehouses.Add(new Warehouse{CompanyId=c.Id,BranchId=b.Id,WarehouseCode="MAIN",WarehouseName="Main Warehouse",IsDefault=true});
        await _db.SaveChangesAsync();
        return Ok(new{company=c,branch=b});
    }
    public record CompanyRequest(string Code,string Name,string? BusinessType=null,string? Phone=null,string? Email=null,string? Address=null,string CurrencyCode="BDT",string TimeZone="Asia/Dhaka");
}