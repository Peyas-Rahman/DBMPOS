using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/users")]
public class UsersController:ControllerBase
{
    private readonly POSDbContext _db;
    public UsersController(POSDbContext db)=>_db=db;
    private Guid CompanyId=>Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet] public async Task<IActionResult> Get()=>Ok(await _db.Users.AsNoTracking().Where(x=>x.CompanyId==CompanyId).Select(x=>new{x.Id,x.Username,x.Email,x.FullName,x.Phone,x.IsActive,x.LastLoginAt,Roles=x.UserRoles.Select(r=>r.Role.RoleName)}).ToListAsync());
    [HttpGet("roles")] public async Task<IActionResult> Roles()=>Ok(await _db.Roles.AsNoTracking().Where(x=>x.IsActive&&(x.CompanyId==null||x.CompanyId==CompanyId)).Select(x=>new{x.Id,x.RoleName,x.Description,x.IsSystemRole}).ToListAsync());
    [HttpPost] public async Task<IActionResult> Create(UserRequest r)
    {
        if(await _db.Users.AnyAsync(x=>x.CompanyId==CompanyId&&x.Username==r.Username))return Conflict(new{message="Username already exists."});
        var u=new User{CompanyId=CompanyId,Username=r.Username,Email=r.Email,FullName=r.FullName,Phone=r.Phone};
        u.PasswordHash=new PasswordHasher<User>().HashPassword(u,r.Password);
        _db.Users.Add(u);await _db.SaveChangesAsync();
        if(r.RoleId.HasValue){
            var role=await _db.Roles.FirstOrDefaultAsync(x=>x.Id==r.RoleId&&x.IsActive&&(x.CompanyId==null||x.CompanyId==CompanyId));
            if(role!=null){_db.UserRoles.Add(new UserRole{UserId=u.Id,RoleId=role.Id,BranchId=r.BranchId});await _db.SaveChangesAsync();}
        }
        return Ok(new{u.Id,u.Username,u.Email,u.FullName});
    }
    [HttpPut("{id:guid}/status")] public async Task<IActionResult> Status(Guid id,[FromBody]bool active)
    {
        var u=await _db.Users.FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==CompanyId);if(u==null)return NotFound();u.IsActive=active;u.UpdatedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok();
    }
    public record UserRequest(string Username,string Email,string FullName,string Password,string? Phone=null,Guid? RoleId=null,Guid? BranchId=null);
}