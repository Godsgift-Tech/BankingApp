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

            // Fonts
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.WHITE);
            var subTitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY);
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

            var creditFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GREEN);
            var debitFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.RED);

            // Title
            var titleTable = new PdfPTable(1) { WidthPercentage = 100 };
            var titleCell = new PdfPCell(new Phrase("First Ally Capital Bank - Transaction History", titleFont))
            {
                BackgroundColor = new BaseColor(0, 102, 204),
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 10,
                Border = Rectangle.NO_BORDER
            };
            titleTable.AddCell(titleCell);
            document.Add(titleTable);

            // Subtitle
            document.Add(new Paragraph($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}", subTitleFont));
            document.Add(new Paragraph(" "));

            // Transaction Table
            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 25, 35, 20, 20 });

            // Headers
            var headerBg = new BaseColor(51, 153, 255);
            string[] headers = { "Date", "Description", "Amount", "Balance" };
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, headerFont))
                {
                    BackgroundColor = headerBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    Padding = 5
                };
                table.AddCell(cell);
            }

            // Data
            bool alternateRow = false;
            foreach (var t in transactions)
            {
                var bgColor = alternateRow ? new BaseColor(230, 240, 255) : BaseColor.WHITE;

                // Date & Description
                table.AddCell(CreateStyledCell(t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), normalFont, bgColor));
                table.AddCell(CreateStyledCell(t.Description, normalFont, bgColor));

                // Amount → Green if credit, Red if debit
                var amountFont = t.Amount >= 0 ? creditFont : debitFont;
                table.AddCell(CreateStyledCell(t.Amount.ToString("C"), amountFont, bgColor, Element.ALIGN_RIGHT));

                // Balance (keep normal black font)
                table.AddCell(CreateStyledCell(t.BalanceAfterTransaction.ToString("C"), normalFont, bgColor, Element.ALIGN_RIGHT));

                alternateRow = !alternateRow;
            }

            document.Add(table);
            document.Close();
            writer.Close();

            return ms.ToArray();
        }

        private PdfPCell CreateStyledCell(string text, Font font, BaseColor bgColor, int alignment = Element.ALIGN_LEFT)
        {
            return new PdfPCell(new Phrase(text, font))
            {
                BackgroundColor = bgColor,
                HorizontalAlignment = alignment,
                Padding = 5
            };
        }

        public byte[] ExportTransactionsToExcel(List<TransactionHistoryDto> transactions)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

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
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 102, 204));
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.White);
            }

            // Data Rows
            int row = 2;
            bool alternate = false;
            foreach (var t in transactions)
            {
                var bgColor = alternate ? System.Drawing.Color.FromArgb(230, 240, 255) : System.Drawing.Color.White;

                ws.Cells[row, 1].Value = t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                ws.Cells[row, 2].Value = t.Description;
                ws.Cells[row, 3].Value = t.Amount;
                ws.Cells[row, 4].Value = t.BalanceAfterTransaction;

                // Background for alternating rows
                using (var range = ws.Cells[row, 1, row, 4])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(bgColor);
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Amount coloring
                ws.Cells[row, 3].Style.Font.Color.SetColor(t.Amount >= 0
                    ? System.Drawing.Color.Green
                    : System.Drawing.Color.Red);

                alternate = !alternate;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
