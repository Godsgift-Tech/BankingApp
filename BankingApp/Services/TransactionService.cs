using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Core.Entities;
using BankingApp.Core.Enums;
using Serilog;

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
            Log.Information("Initiating deposit: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
            {
                Log.Warning("Deposit failed: Account not found. AccountId={AccountId}", dto.AccountId);
                throw new Exception("Account not found");
            }

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

            Log.Information("Deposit successful: AccountId={AccountId}, NewBalance={Balance}", dto.AccountId, account.Balance);
        }

        public async Task WithdrawAsync(WithdrawDto dto)
        {
            Log.Information("Initiating withdrawal: AccountId={AccountId}, Amount={Amount}", dto.AccountId, dto.Amount);

            var account = await _transactionRepository.GetAccountByIdAsync(dto.AccountId);
            if (account == null)
            {
                Log.Warning("Withdrawal failed: Account not found. AccountId={AccountId}", dto.AccountId);
                throw new Exception("Account not found");
            }

            if (account.Balance < dto.Amount)
            {
                Log.Warning("Withdrawal failed: Insufficient funds. AccountId={AccountId}, Balance={Balance}, AttemptedAmount={Amount}",
                    dto.AccountId, account.Balance, dto.Amount);
                throw new Exception("Insufficient balance");
            }

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

            Log.Information("Withdrawal successful: AccountId={AccountId}, NewBalance={Balance}", dto.AccountId, account.Balance);
        }

        public async Task TransferAsync(TransferDto dto)
        {
            Log.Information("Initiating transfer: FromAccountId={FromAccountId}, ToAccountNumber={ToAccountNumber}, Amount={Amount}",
                dto.FromAccountId, dto.ToAccountNumber, dto.Amount);

            var fromAccount = await _transactionRepository.GetAccountByIdAsync(dto.FromAccountId);
            var toAccount = await _transactionRepository.GetAccountByNumberAsync(dto.ToAccountNumber);

            if (fromAccount == null || toAccount == null)
            {
                Log.Warning("Transfer failed: One or both accounts not found. From={FromAccountId}, To={ToAccountNumber}",
                    dto.FromAccountId, dto.ToAccountNumber);
                throw new Exception("One or both accounts not found");
            }

            if (fromAccount.Balance < dto.Amount)
            {
                Log.Warning("Transfer failed: Insufficient funds. FromAccountId={FromAccountId}, Balance={Balance}, Amount={Amount}",
                    dto.FromAccountId, fromAccount.Balance, dto.Amount);
                throw new Exception("Insufficient balance");
            }

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

            Log.Information("Transfer successful: From={FromAccountId}, To={ToAccountId}, Amount={Amount}", fromAccount.Id, toAccount.Id, dto.Amount);
        }

        public async Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryAsync(
            Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
        {
            Log.Information("Retrieving transaction history for AccountId={AccountId}, Page={Page}", accountId, page);

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
