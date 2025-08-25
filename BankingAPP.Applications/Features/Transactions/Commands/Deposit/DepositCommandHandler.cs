using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Deposit
{
    public class DepositCommandHandler : IRequestHandler<DepositCommand, TransactionHistoryDto>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public DepositCommandHandler(
            IAccountRepository accountRepository,
            ITransactionRepository transactionRepository,
            IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<TransactionHistoryDto> Handle(DepositCommand request, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("Received DepositCommand: {@DepositCommand}", request);

                // Fetch account by account number
                var account = await _accountRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);
                if (account == null)
                {
                    Log.Warning("Deposit failed: account {AccountNumber} not found", request.AccountNumber);
                    throw new KeyNotFoundException($"Account with number {request.AccountNumber} not found.");
                }

                // Update balance
                account.Balance += request.Amount;

                // Create transaction
                var transaction = new Transaction
                {
                    AccountId = account.Id,
                    Type = TransactionType.Deposit,
                    Amount = request.Amount,
                    Description = request.Description,
                    Timestamp = DateTime.UtcNow,
                    BalanceAfterTransaction = account.Balance,
                    Status = TransactionStatus.Success
                };

                // Save to DB + invalidate cache
                await _accountRepository.UpdateAsync(account, cancellationToken);
                await _transactionRepository.AddAsync(transaction, cancellationToken);

                await _cache.RemoveAsync($"account:{account.Id}", cancellationToken);
                await _cache.RemoveAsync($"transactions:{account.Id}", cancellationToken);

                Log.Information("Deposit successful for account {AccountNumber}. Transaction ID: {TransactionId}",
                    account.AccountNumber, transaction.Id);

                // Return DTO with currency formatting
                return new TransactionHistoryDto
                {
                    Id = transaction.Id,
                    AccountNumber = account.AccountNumber,
                   // Currency = account.Currency,
                    Currency = string.IsNullOrWhiteSpace(account.Currency) ? "NGN" : account.Currency, // fallback
                    Amount = transaction.Amount,
                   // AmountWithCurrency = $"{account.Currency} {transaction.Amount:N2}",
                    Description = transaction.Description ?? string.Empty,
                    Timestamp = transaction.Timestamp,
                    Type = transaction.Type.ToString(),
                    Status = transaction.Status.ToString(),
                    TargetAccountNumber = transaction.TargetAccountNumber,
                    BalanceAfterTransaction = transaction.BalanceAfterTransaction,
                   // BalanceAfterTransactionWithCurrency = $"{account.Currency} {transaction.BalanceAfterTransaction:N2}"
                };
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled error in DepositCommandHandler for request: {@DepositCommand}", request);
                throw;
            }
        }
    }

}
