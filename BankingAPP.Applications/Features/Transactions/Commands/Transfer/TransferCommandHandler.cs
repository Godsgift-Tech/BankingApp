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
            Log.Information("Initiating transfer from {FromAccountId} to {ToAccountNumber} for amount {Amount}",
                request.FromAccountId, request.ToAccountNumber, request.Amount);

            //  Feltching account by Id
            var fromAccount = await _accountRepository.GetByIdAsync(request.FromAccountId, cancellationToken);
            if (fromAccount == null)
            {
                Log.Warning("Source account {FromAccountId} not found", request.FromAccountId);
                throw new KeyNotFoundException($"Source account {request.FromAccountId} not found.");
            }

            //  Getting destination account
            var toAccount = (await _accountRepository.GetAllAsync(cancellationToken))
                .FirstOrDefault(a => a.AccountNumber == request.ToAccountNumber);

            if (toAccount == null)
            {
                Log.Warning("Target account {ToAccountNumber} not found", request.ToAccountNumber);
                throw new KeyNotFoundException($"Target account {request.ToAccountNumber} not found.");
            }

            //  Checking  balance capacity
            if (fromAccount.Balance < request.Amount)
            {
                Log.Warning("Insufficient funds in account {FromAccountId}", request.FromAccountId);
                throw new InvalidOperationException("Insufficient balance.");
            }

            //  Deduct and add balance depending on the transfer
            fromAccount.Balance -= request.Amount;
            toAccount.Balance += request.Amount;

            //  Create transactions (Both are Transfer type) either incomming or outgoing
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

            //  Save to DB
            await _transactionRepository.AddAsync(debitTransaction, cancellationToken);
            await _transactionRepository.AddAsync(creditTransaction, cancellationToken);
            await _accountRepository.UpdateAsync(fromAccount, cancellationToken);
            await _accountRepository.UpdateAsync(toAccount, cancellationToken);

            //  Invalidate Redis cache for accounts and transactions
            await _cache.RemoveAsync("accounts:all", cancellationToken);
            await _cache.RemoveAsync($"transactions:{fromAccount.Id}", cancellationToken);
            await _cache.RemoveAsync($"transactions:{toAccount.Id}", cancellationToken);

            Log.Information("Transfer successful from {FromAccountId} to {ToAccountNumber} for amount {Amount}",
                request.FromAccountId, request.ToAccountNumber, request.Amount);

            return true;
        }
    }





}