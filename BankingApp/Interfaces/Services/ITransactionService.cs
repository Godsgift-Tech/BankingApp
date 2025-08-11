using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;
using BankingApp.Core.Entities;

namespace BankingApp.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        Task<Transaction> DepositAsync(DepositDto dto);
        Task<Transaction> WithdrawAsync(WithdrawDto dto);
        Task<Transaction> TransferAsync(TransferDto dto);

        Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryByAccountIdAsync(
            Guid accountId,
            int pageNumber,
            int pageSize,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken
        );

        Task<PagedResult<TransactionHistoryDto>> GetAccountHistoryByAccountNumberAsync(
          string accountNumber,
          int pageNumber,
          int pageSize,
          DateTime? fromDate,
          DateTime? toDate,
          CancellationToken cancellationToken
      );

    }
}
