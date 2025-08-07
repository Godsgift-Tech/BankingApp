using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Services
{


    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task DepositAsync(DepositDto dto)
        {
            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
                throw new Exception("Account not found");

            account.Balance += dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();
        }

        public async Task WithdrawAsync(WithdrawDto dto)
        {
            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
                throw new Exception("Account not found");

            if (account.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

            account.Balance -= dto.Amount;

            var transaction = new Transaction
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(account);
            await _transactionRepository.AddTransactionAsync(transaction);
            await _transactionRepository.SaveChangesAsync();
        }

        public async Task TransferAsync(TransferDto dto)
        {
            var fromAccount = await _transactionRepository.GetAccountByIdAsync(dto.FromAccountId);
            var toAccount = await _transactionRepository.GetAccountByNumberAsync(dto.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
                throw new Exception("One or both accounts not found");

            if (fromAccount.Balance < dto.Amount)
                throw new Exception("Insufficient balance");

            fromAccount.Balance -= dto.Amount;
            toAccount.Balance += dto.Amount;

            var debit = new Transaction
            {
                AccountId = fromAccount.Id,
                Amount = dto.Amount,
                Description = dto.Description,
                Type = TransactionType.Transfer,
                TargetAccountNumber = toAccount.AccountNumber,
                Status = TransactionStatus.Success
            };

            var credit = new Transaction
            {
                AccountId = toAccount.Id,
                Amount = dto.Amount,
                Description = "Received Transfer",
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success
            };

            await _transactionRepository.UpdateAccountAsync(fromAccount);
            await _transactionRepository.UpdateAccountAsync(toAccount);
            await _transactionRepository.AddTransactionAsync(debit);
            await _transactionRepository.AddTransactionAsync(credit);
            await _transactionRepository.SaveChangesAsync();
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryAsync(
      Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            var (transactions, totalCount) = await _transactionRepository.GetPagedTransactionsByAccountIdAsync(
                accountId, page, pageSize, fromDate, toDate, cancellationToken);

            return new PagedResult<TransactionHistoryDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalCount,
                Items = transactions.Select(t => new TransactionHistoryDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description!,
                    Timestamp = t.Timestamp,
                    Type = t.Type,
                    Status = t.Status,
                    TargetAccountNumber = t.TargetAccountNumber
                }).ToList()
            };
        }


    }


}
