using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Withdraw
{
    public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, TransactionHistoryDto>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public WithdrawCommandHandler(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<TransactionHistoryDto> Handle(WithdrawCommand request, CancellationToken cancellationToken)
        {
            Log.Information("Starting withdrawal from account {AccountId} for amount {Amount}",
                request.AccountId, request.Amount);

            // Feltching the account by Id
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
            {
                Log.Warning("Account {AccountId} not found", request.AccountId);
                throw new KeyNotFoundException($"Account with ID {request.AccountId} not found.");
            }

            //  Checking for balance sufficiency
            if (account.Balance < request.Amount)
            {
                Log.Warning("Insufficient funds for withdrawal from account {AccountId}", request.AccountId);
                throw new InvalidOperationException("Insufficient balance.");
            }

            //  Deduct from balance for withdraw
            account.Balance -= request.Amount;

            //  Create transaction
            var transaction = new Transaction
            {
                AccountId = account.Id,
                Type = TransactionType.Withdrawal,
                Amount = request.Amount,
                Description = request.Description,
                Timestamp = DateTime.UtcNow,
                BalanceAfterTransaction = account.Balance,
                Status = TransactionStatus.Success
            };

            //  Saving changes
            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            // Invalidate relevant cache entries
            var accountCacheKey = $"account:{account.Id}";
            var transactionsCacheKey = $"transactions:{account.Id}";

            await _cache.RemoveAsync(accountCacheKey, cancellationToken);
            await _cache.RemoveAsync(transactionsCacheKey, cancellationToken);

            Log.Information("Withdrawal successful from account {AccountId} for amount {Amount}",
                request.AccountId, request.Amount);

            // Return response DTO
            return new TransactionHistoryDto
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                Description = transaction.Description ?? string.Empty,
                Timestamp = transaction.Timestamp,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                TargetAccountNumber = transaction.TargetAccountNumber,
                BalanceAfterTransaction = transaction.BalanceAfterTransaction
            };
        }
    }
}
