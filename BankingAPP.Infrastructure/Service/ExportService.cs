using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;

namespace BankingAPP.Infrastructure.Service
{
    public class ExportService : IExportService
    {
        public byte[] ExportTransactionsToPdf(List<TransactionHistoryDto> transactions)
        {
            using var ms = new MemoryStream();
            var document = new Document(PageSize.A4, 25, 25, 30, 30);
            var writer = PdfWriter.GetInstance(document, ms);

            document.Open();

            var font = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

            document.Add(new Paragraph("Transaction History", boldFont));
            document.Add(new Paragraph($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}", font));
            document.Add(new Paragraph(" "));

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 25, 35, 20, 20 });

            // Headers
            table.AddCell(new PdfPCell(new Phrase("Date", boldFont)));
            table.AddCell(new PdfPCell(new Phrase("Description", boldFont)));
            table.AddCell(new PdfPCell(new Phrase("Amount", boldFont)));
            table.AddCell(new PdfPCell(new Phrase("Balance", boldFont)));

            // Data
            foreach (var t in transactions)
            {
                table.AddCell(new Phrase(t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), font));
                table.AddCell(new Phrase(t.Description, font));
                table.AddCell(new Phrase(t.Amount.ToString("C"), font));
                table.AddCell(new Phrase(t.BalanceAfterTransaction.ToString("C"), font));
            }

            document.Add(table);
            document.Close();
            writer.Close();

            return ms.ToArray();
        }

        public byte[] ExportTransactionsToExcel(List<TransactionHistoryDto> transactions)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Transactions");

            // Headers
            ws.Cells[1, 1].Value = "Date";
            ws.Cells[1, 2].Value = "Description";
            ws.Cells[1, 3].Value = "Amount";
            ws.Cells[1, 4].Value = "Balance";

            using (var range = ws.Cells[1, 1, 1, 4])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            // Data
            int row = 2;
            foreach (var t in transactions)
            {
                ws.Cells[row, 1].Value = t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cells[row, 2].Value = t.Description;
                ws.Cells[row, 3].Value = t.Amount;
                ws.Cells[row, 4].Value = t.BalanceAfterTransaction;
                row++;
            }

            ws.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
