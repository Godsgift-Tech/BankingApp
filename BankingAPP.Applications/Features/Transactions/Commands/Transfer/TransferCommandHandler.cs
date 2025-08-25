using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace BankingAPP.Applications.Features.Transactions.Commands.Transfer
{
    public class TransferCommandHandler : IRequestHandler<TransferCommand, bool>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public TransferCommandHandler(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<bool> Handle(TransferCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("Initiating transfer from {FromAccountNumber} to {ToAccountNumber} for amount {Amount}",
                    request.FromAccountNumber, request.ToAccountNumber, request.Amount);

                // Fetch source account
                var fromAccount = await _accountRepository.GetByAccountNumberAsync(request.FromAccountNumber, cancellationToken);
                if (fromAccount == null)
                {
                    Log.Warning("Source account {FromAccountNumber} not found", request.FromAccountNumber);
                    throw new KeyNotFoundException($"Source account {request.FromAccountNumber} not found.");
                }

                // Fetch destination account
                var toAccount = await _accountRepository.GetByAccountNumberAsync(request.ToAccountNumber, cancellationToken);
                if (toAccount == null)
                {
                    Log.Warning("Target account {ToAccountNumber} not found", request.ToAccountNumber);
                    throw new KeyNotFoundException($"Target account {request.ToAccountNumber} not found.");
                }

                // Check balance
                if (fromAccount.Balance < request.Amount)
                {
                    Log.Warning("Insufficient funds in account {FromAccountNumber}", request.FromAccountNumber);
                    throw new InvalidOperationException("Insufficient balance.");
                }

                // Update balances
                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;

                // Create transactions
                var debitTransaction = new Transaction
                {
                    AccountId = fromAccount.Id,
                    Type = TransactionType.Transfer,
                    Amount = request.Amount,
                    Description = request.Description ?? "Transfer",
                    TargetAccountNumber = request.ToAccountNumber,
                    BalanceAfterTransaction = fromAccount.Balance
                };

                var creditTransaction = new Transaction
                {
                    AccountId = toAccount.Id,
                    Type = TransactionType.Transfer,
                    Amount = request.Amount,
                    Description = $"Transfer from {fromAccount.AccountNumber}",
                    TargetAccountNumber = fromAccount.AccountNumber,
                    BalanceAfterTransaction = toAccount.Balance
                };

                // Save to repository
                await _transactionRepository.AddAsync(debitTransaction, cancellationToken);
                await _transactionRepository.AddAsync(creditTransaction, cancellationToken);
                await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
                await _accountRepository.UpdateAsync(toAccount, cancellationToken);

                // Invalidate cache
                await _cache.RemoveAsync($"transactions:{fromAccount.Id}", cancellationToken);
                await _cache.RemoveAsync($"transactions:{toAccount.Id}", cancellationToken);
                await _cache.RemoveAsync("accounts:all", cancellationToken);

                Log.Information("Transfer successful from {FromAccountNumber} to {ToAccountNumber} for amount {Amount}",
                    request.FromAccountNumber, request.ToAccountNumber, request.Amount);

                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled error in TransferCommandHandler for transfer {@TransferCommand}", request);
                throw;
            }
        }
    }
}
