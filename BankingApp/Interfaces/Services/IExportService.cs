using BankingApp.Application.DTO.Transactions;

namespace BankingApp.Application.Interfaces.Services
{
    public interface IExportService
    {
        byte[] ExportTransactionsToPdf(List<TransactionHistoryDto> transactions);
        byte[] ExportTransactionsToExcel(List<TransactionHistoryDto> transactions);
    }
}
