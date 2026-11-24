using BuildingBlock.Application.Behaviors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPark.Infrastructure.Configuration;

namespace NPark.Infrastructure.Services;

internal sealed class TransactionCommitVerifier(NParkDBContext db, IServiceScopeFactory scopes) : IVerifyTransactionCommit
{
    public async Task RecordAsync(Guid transactionId, CancellationToken ct) =>
        await db.AddAsync(new TransactionReceipt(transactionId), ct);

    public async Task<bool> WasCommittedAsync(Guid transactionId, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var verificationDb = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        return await verificationDb.Set<TransactionReceipt>().AnyAsync(x => x.Id == transactionId, ct);
    }
}
