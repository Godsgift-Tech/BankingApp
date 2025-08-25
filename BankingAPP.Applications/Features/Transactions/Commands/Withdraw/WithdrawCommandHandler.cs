using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System;
using System.Threading;
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
            Log.Information("Starting withdrawal from account {AccountNumber} for amount {Amount}",
                request.AccountNumber, request.Amount);

            // Fetch the account by account number
            var account = await _accountRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);
            if (account == null)
            {
                Log.Warning("Account {AccountNumber} not found", request.AccountNumber);
                throw new KeyNotFoundException($"Account with number {request.AccountNumber} not found.");
            }

            // Check for sufficient balance
            if (account.Balance < request.Amount)
            {
                Log.Warning("Insufficient funds for withdrawal from account {AccountNumber}", request.AccountNumber);
                throw new InvalidOperationException("Insufficient balance.");
            }

            // Deduct from balance
            account.Balance -= request.Amount;

            // Create transaction
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

            // Save changes
            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            // Invalidate cache
            var accountCacheKey = $"account:{account.Id}";
            var transactionsCacheKey = $"transactions:{account.Id}";

            await _cache.RemoveAsync(accountCacheKey, cancellationToken);
            await _cache.RemoveAsync(transactionsCacheKey, cancellationToken);

            Log.Information("Withdrawal successful from account {AccountNumber} for amount {Amount}",
                request.AccountNumber, request.Amount);

            // Return DTO
            return new TransactionHistoryDto
            {
                Id = transaction.Id,
                AccountNumber = account.AccountNumber,
              //  Currency = account.Currency,
                Currency = string.IsNullOrWhiteSpace(account.Currency) ? "NGN" : account.Currency, // fallback
                Amount = transaction.Amount,
              //  AmountWithCurrency = $"{account.Currency} {transaction.Amount:N2}",
                Description = transaction.Description ?? string.Empty,
                Timestamp = transaction.Timestamp,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                TargetAccountNumber = transaction.TargetAccountNumber,
                BalanceAfterTransaction = transaction.BalanceAfterTransaction,
              //  BalanceAfterTransactionWithCurrency = $"{account.Currency} {transaction.BalanceAfterTransaction:N2}"
            };
        }
    }
}
