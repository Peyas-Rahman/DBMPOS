using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/organization")]
public class OrganizationController:ControllerBase
{
    private readonly POSDbContext _db;
    public OrganizationController(POSDbContext db)=>_db=db;
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("company")] public async Task<IActionResult> Company()=>Ok(await _db.Companies.AsNoTracking().FirstAsync(x=>x.Id==CompanyId));
    [HttpGet("branches")] public async Task<IActionResult> Branches()=>Ok(await _db.Branches.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.IsActive).OrderBy(x=>x.BranchName).ToListAsync());
    [HttpPost("branches")] public async Task<IActionResult> Branch(BranchRequest r)
    {
        if(await _db.Branches.AnyAsync(x=>x.CompanyId==CompanyId&&x.BranchCode==r.Code))return Conflict(new{message="Branch code already exists."});
        var b=new Branch{CompanyId=CompanyId,BranchCode=r.Code,BranchName=r.Name,Phone=r.Phone,Address=r.Address,IsHeadOffice=r.IsHeadOffice};
        _db.Branches.Add(b);await _db.SaveChangesAsync();return Ok(b);
    }
    [HttpPut("branches/{id:guid}")] public async Task<IActionResult> UpdateBranch(Guid id,BranchRequest r)
    {
        var b=await _db.Branches.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId&&x.IsActive); if(b==null)return NotFound();
        if(await _db.Branches.AnyAsync(x=>x.CompanyId==CompanyId&&x.BranchCode==r.Code&&x.Id!=id))return Conflict(new{message="Branch code already exists."});
        b.BranchCode=r.Code.Trim();b.BranchName=r.Name.Trim();b.Phone=r.Phone;b.Address=r.Address;b.IsHeadOffice=r.IsHeadOffice;b.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok(b);
    }
    [HttpDelete("branches/{id:guid}")] public async Task<IActionResult> DeleteBranch(Guid id)
    {
        var b=await _db.Branches.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId&&x.IsActive); if(b==null)return NotFound();
        if(await _db.Sales.AnyAsync(x=>x.CompanyId==CompanyId&&x.BranchId==id))return Conflict(new{message="Branch has sales history and cannot be deleted."});
        b.IsActive=false;b.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok(new{message="Branch deleted."});
    }
    [HttpGet("warehouses")] public async Task<IActionResult> Warehouses([FromQuery]Guid? branchId)=>Ok(await _db.Warehouses.AsNoTracking().Where(x=>x.CompanyId==CompanyId&&x.IsActive&&(!branchId.HasValue||x.BranchId==branchId)).OrderBy(x=>x.WarehouseName).ToListAsync());
    [HttpPost("warehouses")] public async Task<IActionResult> Warehouse(WarehouseRequest r)
    {
        if(!await _db.Branches.AnyAsync(x=>x.Id==r.BranchId&&x.CompanyId==CompanyId))return BadRequest(new{message="Invalid branch."});
        if(await _db.Warehouses.AnyAsync(x=>x.CompanyId==CompanyId&&x.BranchId==r.BranchId&&x.WarehouseCode==r.Code))return Conflict(new{message="Warehouse code already exists."});
        var w=new Warehouse{CompanyId=CompanyId,BranchId=r.BranchId,WarehouseCode=r.Code,WarehouseName=r.Name,IsDefault=r.IsDefault};
        _db.Warehouses.Add(w);await _db.SaveChangesAsync();return Ok(w);
    }
    [HttpPut("warehouses/{id:guid}")] public async Task<IActionResult> UpdateWarehouse(Guid id,WarehouseRequest r)
    {
        var w=await _db.Warehouses.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId&&x.IsActive); if(w==null)return NotFound();
        if(!await _db.Branches.AnyAsync(x=>x.Id==r.BranchId&&x.CompanyId==CompanyId&&x.IsActive))return BadRequest(new{message="Invalid branch."});
        if(await _db.Warehouses.AnyAsync(x=>x.CompanyId==CompanyId&&x.BranchId==r.BranchId&&x.WarehouseCode==r.Code&&x.Id!=id))return Conflict(new{message="Warehouse code already exists."});
        w.BranchId=r.BranchId;w.WarehouseCode=r.Code.Trim();w.WarehouseName=r.Name.Trim();w.IsDefault=r.IsDefault;w.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok(w);
    }
    [HttpDelete("warehouses/{id:guid}")] public async Task<IActionResult> DeleteWarehouse(Guid id)
    {
        var w=await _db.Warehouses.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId&&x.IsActive); if(w==null)return NotFound();
        if(await _db.Sales.AnyAsync(x=>x.CompanyId==CompanyId&&x.WarehouseId==id))return Conflict(new{message="Warehouse has sales history and cannot be deleted."});
        w.IsActive=false;w.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok(new{message="Warehouse deleted."});
    }
    public record BranchRequest(string Code,string Name,string? Phone=null,string? Address=null,bool IsHeadOffice=false);
    public record WarehouseRequest(Guid BranchId,string Code,string Name,bool IsDefault=false);
}