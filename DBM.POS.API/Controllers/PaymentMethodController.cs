using System.Security.Claims;using DBM.POS.Domain.Entities;using DBM.POS.Infrastructure.Data;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace DBM.POS.API.Controllers;
[ApiController,Authorize,Route("api/payment-methods")]
public class PaymentMethodController:ControllerBase{private readonly POSDbContext _db;public PaymentMethodController(POSDbContext db)=>_db=db;private Guid C=>Guid.Parse(User.FindFirstValue("companyId")!);
[HttpGet]public async Task<IActionResult>Get()=>Ok(await _db.PaymentMethods.AsNoTracking().Where(x=>x.CompanyId==C&&x.IsActive).OrderBy(x=>x.Name).ToListAsync());
[HttpPost]public async Task<IActionResult>Create(RequestDto r){if(await _db.PaymentMethods.AnyAsync(x=>x.CompanyId==C&&x.Code==r.Code))return Conflict(new{message="Payment method code exists."});var p=new PaymentMethod{CompanyId=C,Code=r.Code,Name=r.Name,IsCash=r.IsCash,IsDefault=r.IsDefault};_db.PaymentMethods.Add(p);await _db.SaveChangesAsync();return Ok(p);}public record RequestDto(string Code,string Name,bool IsCash=false,bool IsDefault=false);}
