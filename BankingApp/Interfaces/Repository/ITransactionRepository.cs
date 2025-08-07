using BankingApp.Core.Entities;

namespace BankingApp.Application.Interfaces.Repository
{
    public interface ITransactionRepository
    {
        Task<Account?> GetAccountByIdAsync(Guid accountId);
        Task<Account?> GetAccountByNumberAsync(string accountNumber);
        Task UpdateAccountAsync(Account account);
        Task AddTransactionAsync(Transaction transaction);
        Task SaveChangesAsync();
        Task<(List<Transaction> Transactions, int TotalCount)> GetPagedTransactionsByAccountIdAsync(
      Guid accountId,
      int page,
      int pageSize,
      DateTime? fromDate,
      DateTime? toDate,
      CancellationToken cancellationToken);



    }
}
