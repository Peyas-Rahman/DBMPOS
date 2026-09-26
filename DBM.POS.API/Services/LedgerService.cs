using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
namespace DBM.POS.API.Services;
public class LedgerService
{
    private readonly POSDbContext _db;
    public LedgerService(POSDbContext db)=>_db=db;
    public async Task<decimal> CustomerBalance(Guid companyId, Guid customerId) => await _db.CustomerLedgers.Where(x=>x.CompanyId==companyId&&x.CustomerId==customerId).OrderByDescending(x=>x.TransactionDate).ThenByDescending(x=>x.CreatedAt).Select(x=>(decimal?)x.Balance).FirstOrDefaultAsync() ?? await _db.Customers.Where(x=>x.CompanyId==companyId&&x.Id==customerId).Select(x=>x.OpeningDue).FirstOrDefaultAsync();
    public async Task<decimal> SupplierBalance(Guid companyId, Guid supplierId) => await _db.SupplierLedgers.Where(x=>x.CompanyId==companyId&&x.SupplierId==supplierId).OrderByDescending(x=>x.TransactionDate).ThenByDescending(x=>x.CreatedAt).Select(x=>(decimal?)x.Balance).FirstOrDefaultAsync() ?? await _db.Suppliers.Where(x=>x.CompanyId==companyId&&x.Id==supplierId).Select(x=>x.OpeningDue).FirstOrDefaultAsync();
    public async Task AddCustomer(Guid companyId, Guid customerId, Guid? branchId, string type, Guid? referenceId, string? referenceNo, decimal debit, decimal credit, string? description, Guid? userId)
    {
        var old=await CustomerBalance(companyId,customerId); var balance=old+debit-credit;
        _db.CustomerLedgers.Add(new CustomerLedger{CompanyId=companyId,CustomerId=customerId,BranchId=branchId,TransactionType=type,ReferenceId=referenceId,ReferenceNo=referenceNo,Debit=debit,Credit=credit,Balance=balance,Description=description,UserId=userId});
    }
    public async Task AddSupplier(Guid companyId, Guid supplierId, Guid? branchId, string type, Guid? referenceId, string? referenceNo, decimal debit, decimal credit, string? description, Guid? userId)
    {
        var old=await SupplierBalance(companyId,supplierId); var balance=old+debit-credit;
        _db.SupplierLedgers.Add(new SupplierLedger{CompanyId=companyId,SupplierId=supplierId,BranchId=branchId,TransactionType=type,ReferenceId=referenceId,ReferenceNo=referenceNo,Debit=debit,Credit=credit,Balance=balance,Description=description,UserId=userId});
    }
}
