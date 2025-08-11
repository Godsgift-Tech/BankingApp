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
                // Log incoming request
                Log.Information("Received DepositCommand: {@DepositCommand}", request);

                //  Get the account by Id
                var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
                if (account == null)
                {
                    Log.Warning("Deposit failed: account {AccountId} not found", request.AccountId);
                    throw new KeyNotFoundException($"Account with ID {request.AccountId} not found.");
                }

                //  Update account balance
                account.Balance += request.Amount;
                Log.Information("Account {AccountId} new balance after deposit: {Balance}",
                    account.Id, account.Balance);

                //  Create transaction
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

                try
                {
                    //  Save updates
                    await _accountRepository.UpdateAsync(account, cancellationToken);
                    await _transactionRepository.AddAsync(transaction, cancellationToken);

                    //  Invalidate caches
                    var accountCacheKey = $"account:{account.Id}";
                    var transactionsCacheKey = $"transactions:{account.Id}";

                    await _cache.RemoveAsync(accountCacheKey, cancellationToken);
                    await _cache.RemoveAsync(transactionsCacheKey, cancellationToken);

                    Log.Information("Deposit successful for account {AccountId}. Transaction ID: {TransactionId}",
                        account.Id, transaction.Id);
                }
                catch (Exception dbEx)
                {
                    Log.Error(dbEx, "Database/cache error during deposit for account {AccountId}", account.Id);
                    throw;
                }

                //  Returning DTO
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
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled error in DepositCommandHandler for request: {@DepositCommand}", request);
                throw;
            }
        }
    }
}
