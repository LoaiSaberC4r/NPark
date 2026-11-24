using System.Data;
using BuildingBlock.Application.Repositories;
using BuildingBlock.Domain.Results;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlock.Application.Behaviors;

public interface ITransactionalRequest { }

public interface IVerifyTransactionCommit
{
    Task<bool> WasCommittedAsync(Guid transactionId, CancellationToken cancellationToken);
    Task RecordAsync(Guid transactionId, CancellationToken cancellationToken);
}

public sealed class TransactionScopeState
{
    internal bool Active { get; set; }
    internal bool RollbackOnly { get; set; }
}

public sealed class TransactionBehavior<TRequest, TResponse>(
    IServiceProvider services,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ITransactionalRequest)
            return await next();

        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var db = services.GetRequiredService<IDbContextProvider>().Context;
        var state = services.GetRequiredService<TransactionScopeState>();
        if (state.Active)
        {
            try
            {
                var nested = await next();
                state.RollbackOnly |= nested is not IResult { IsSuccess: true };
                return nested;
            }
            catch
            {
                state.RollbackOnly = true;
                throw;
            }
        }

        if (db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException("The existing transaction has no pipeline owner.");

        var verification = services.GetRequiredService<IVerifyTransactionCommit>();
        var transactionId = Guid.NewGuid();
        await using var transaction = await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        state.Active = true;
        try
        {
            var response = await next();
            if (response is not IResult result)
                throw new InvalidOperationException("Transactional commands must return IResult.");

            if (result.IsFailure)
            {
                await RollbackAsync(transaction);
                db.ChangeTracker.Clear();
                return response;
            }

            if (state.RollbackOnly)
                throw new InvalidOperationException("A nested transactional command failed.");

            ct.ThrowIfCancellationRequested();
            await verification.RecordAsync(transactionId, ct);
            await unitOfWork.SaveChangesAsync(ct);
            ct.ThrowIfCancellationRequested();
            try
            {
                await transaction.CommitAsync(CancellationToken.None);
            }
            catch (Exception commitError)
            {
                db.ChangeTracker.Clear();
                await RollbackAsync(transaction);
                try
                {
                    using var verificationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    if (await verification.WasCommittedAsync(transactionId, verificationTimeout.Token))
                        return response;
                }
                catch (Exception verificationError)
                {
                    logger.LogError(verificationError, "Could not verify transaction {TransactionId}", transactionId);
                }
                logger.LogError(commitError, "Transaction {TransactionId} did not report a successful commit", transactionId);
                throw;
            }
            return response;
        }
        catch
        {
            await RollbackAsync(transaction);
            db.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            state.Active = false;
            state.RollbackOnly = false;
        }
    }

    private async Task RollbackAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Transaction rollback could not be confirmed");
        }
    }
}
