using BankingApp.Application.DTO.Common;
using BankingApp.Application.DTO.Transactions;

namespace BankingApp.Application.Interfaces.Services
{
    public interface ITransactionService
    {
        Task DepositAsync(DepositDto dto);
        Task WithdrawAsync(WithdrawDto dto);
        Task TransferAsync(TransferDto dto);
        Task<PagedResult<TransactionHistoryDto>> GetTransactionHistoryAsync(
    Guid accountId, int page, int pageSize, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken);



    }
}
