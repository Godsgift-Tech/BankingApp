using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            //  Get the account by Id
            var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account == null)
                throw new KeyNotFoundException($"Account with ID {request.AccountId} not found.");

            // Recieving balance update
            account.Balance += request.Amount;

            // 3. Create transaction
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

            // We update the account balance and add the transaction to the repository
            await _accountRepository.UpdateAsync(account, cancellationToken);
            await _transactionRepository.AddAsync(transaction, cancellationToken);

            //  updating with Redis cache
            var accountCacheKey = $"account:{account.Id}";
            var transactionsCacheKey = $"transactions:{account.Id}";

            // Update account cache
            await _cache.RemoveAsync(accountCacheKey, cancellationToken);

            // Remove transaction history cache to ensure fresh data for next query
            await _cache.RemoveAsync(transactionsCacheKey, cancellationToken);

            //  Returning response DTO
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
