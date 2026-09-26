using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DBM.POS.API.Controllers;

[ApiController, Authorize, Route("api/sync-status")]
public class SyncStatusController : ControllerBase
{
    private readonly POSDbContext _db;
    public SyncStatusController(POSDbContext db) => _db = db;
    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var branchId = Guid.TryParse(User.FindFirstValue("branchId"), out var b) ? b : (Guid?)null;
        var q = _db.SyncQueueItems.Where(x => x.CompanyId == CompanyId);
        if (branchId.HasValue) q = q.Where(x => x.BranchId == branchId.Value);
        return Ok(new
        {
            pending = await q.CountAsync(x => x.Status == "Pending"),
            failed = await q.CountAsync(x => x.Status == "Failed"),
            synced = await q.CountAsync(x => x.Status == "Synced"),
            conflicts = await _db.SyncConflicts.CountAsync(x => x.CompanyId == CompanyId && x.Status == "Open"),
            lastSync = await q.Where(x => x.SyncedAt != null).OrderByDescending(x => x.SyncedAt).Select(x => x.SyncedAt).FirstOrDefaultAsync()
        });
    }
}
