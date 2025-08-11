using BankingAPP.Applications.Features.Transactions.DTO;


namespace BankingAPP.Applications.Features.Common.Interfaces
{
    public interface IExportService
    {
        byte[] ExportTransactionsToPdf(List<TransactionHistoryDto> transactions);
        byte[] ExportTransactionsToExcel(List<TransactionHistoryDto> transactions);
    }

}
