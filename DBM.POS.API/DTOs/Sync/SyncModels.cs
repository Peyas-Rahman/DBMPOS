namespace DBM.POS.API.DTOs.Sync;

public sealed record SyncEnvelope(
    Guid TransactionId,
    Guid CompanyId,
    Guid BranchId,
    Guid? BranchServerId,
    string EntityType,
    string Operation,
    DateTime CreatedAt,
    string Payload);

public sealed record SyncPushRequest(List<SyncEnvelope> Items);
public sealed record SyncAckRequest(List<Guid> TransactionIds);
public sealed record SyncPullRequest(Guid CompanyId, Guid BranchId, long AfterSequence, int Take = 200);
