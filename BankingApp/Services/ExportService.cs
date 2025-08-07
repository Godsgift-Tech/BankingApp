using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace BankingApp.Application.Services
{
    public class ExportService : IExportService
    {
        public byte[] ExportTransactionsToPdf(List<TransactionHistoryDto> transactions)
        {
            var document = new TransactionDocument(transactions);
            return document.GeneratePdf();
        }

        public byte[] ExportTransactionsToExcel(List<TransactionHistoryDto> transactions)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");

            // Header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Amount";
            worksheet.Cell(1, 3).Value = "Type";
            worksheet.Cell(1, 4).Value = "Description";

            for (int i = 0; i < transactions.Count; i++)
            {
                var tx = transactions[i];
                worksheet.Cell(i + 2, 1).Value = tx.Timestamp;
                worksheet.Cell(i + 2, 2).Value = tx.Amount;
                worksheet.Cell(i + 2, 3).Value = tx.Type.ToString();
                worksheet.Cell(i + 2, 4).Value = tx.Description ?? "-";
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
