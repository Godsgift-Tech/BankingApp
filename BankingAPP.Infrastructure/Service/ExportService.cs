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

            // Transaction Table - now 5 columns
            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new float[] { 20, 30, 15, 15, 20 });

            // Headers
            var headerBg = new BaseColor(51, 153, 255);
            string[] headers = { "Date", "Description", "Transaction", "Amount", "Balance" };
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

                table.AddCell(CreateStyledCell(t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), normalFont, bgColor));
                table.AddCell(CreateStyledCell(t.Description, normalFont, bgColor));

                // Color for transaction type
                Font typeFont = normalFont;
                switch (t.Type.ToLower())
                {
                    case "withdraw":
                        typeFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.RED);
                        break;
                    case "transfer":
                        typeFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(0, 0, 255));
                        break;
                    case "deposit":
                        typeFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GREEN);
                        break;
                }
                table.AddCell(CreateStyledCell(t.Type, typeFont, bgColor));

                // Amount with color + currency
                Font amountFont;
                switch (t.Type.ToLower())
                {
                    case "withdraw":
                        amountFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.RED);
                        break;
                    case "transfer":
                        amountFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, new BaseColor(0, 0, 255));
                        break;
                    case "deposit":
                        amountFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GREEN);
                        break;
                    default:
                        amountFont = normalFont;
                        break;
                }

                table.AddCell(CreateStyledCell(t.AmountWithCurrency, amountFont, bgColor, Element.ALIGN_RIGHT));
                table.AddCell(CreateStyledCell(t.BalanceAfterTransactionWithCurrency, normalFont, bgColor, Element.ALIGN_RIGHT));

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

            // Headers - now 5 columns
            ws.Cells[1, 1].Value = "Date";
            ws.Cells[1, 2].Value = "Description";
            ws.Cells[1, 3].Value = "Type";
            ws.Cells[1, 4].Value = "Amount";
            ws.Cells[1, 5].Value = "Balance";

            using (var range = ws.Cells[1, 1, 1, 5])
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
                ws.Cells[row, 3].Value = t.Type;
                ws.Cells[row, 4].Value = t.AmountWithCurrency;  // ✅ with currency
                ws.Cells[row, 5].Value = t.BalanceAfterTransactionWithCurrency; // ✅ with currency

                using (var range = ws.Cells[row, 1, row, 5])
                {
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(bgColor);
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }

                // Align Amount & Balance to the right
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Color for transaction type column
                System.Drawing.Color typeColor;
                switch (t.Type.ToLower())
                {
                    case "withdraw":
                        typeColor = System.Drawing.Color.Red;
                        break;
                    case "transfer":
                        typeColor = System.Drawing.Color.Blue;
                        break;
                    case "deposit":
                        typeColor = System.Drawing.Color.Green;
                        break;
                    default:
                        typeColor = System.Drawing.Color.Black;
                        break;
                }
                ws.Cells[row, 3].Style.Font.Color.SetColor(typeColor);

                // Color for amount column
                System.Drawing.Color amountColor;
                switch (t.Type.ToLower())
                {
                    case "withdraw":
                        amountColor = System.Drawing.Color.Red;
                        break;
                    case "transfer":
                        amountColor = System.Drawing.Color.Blue;
                        break;
                    case "deposit":
                        amountColor = System.Drawing.Color.Green;
                        break;
                    default:
                        amountColor = System.Drawing.Color.Black;
                        break;
                }
                ws.Cells[row, 4].Style.Font.Color.SetColor(amountColor);

                alternate = !alternate;
                row++;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }
    }
}
