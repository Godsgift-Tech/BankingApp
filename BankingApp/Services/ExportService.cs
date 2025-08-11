using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Services;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BankingApp.Application.Services
{
    public class ExportService : IExportService
    {
        public ExportService()
        {
            //  QuestPDF uses the free community license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] ExportTransactionsToPdf(List<TransactionHistoryDto> transactions)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    // HEADER
                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().AlignCenter().Text("First Ally Capital Bank Transaction Statement")
                                .FontSize(20)
                                .SemiBold()
                                .FontColor(Colors.Blue.Medium);
                            col.Item().AlignCenter().Text($"Generated on {DateTime.Now:dd MMM yyyy, HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });

                    // CONTENT
                    page.Content().Element(content =>
                    {
                        content.PaddingVertical(10).Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(100); // Date
                                columns.ConstantColumn(80);  // Type
                                columns.RelativeColumn();    // Description
                                columns.ConstantColumn(80);  // Amount
                                columns.ConstantColumn(100); // Balance
                            });

                            // Table header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Date").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Type").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Description").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Amount").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(Colors.Blue.Medium).Padding(5)
                                    .Text("Balance").FontColor(Colors.White).SemiBold();
                            });

                            // Table rows with alternating colors
                            bool alternate = false;
                            foreach (var t in transactions)
                            {
                                var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                                alternate = !alternate;

                                table.Cell().Background(bgColor).Padding(5)
                                    .Text(t.Timestamp.ToString("yyyy-MM-dd"));
                                table.Cell().Background(bgColor).Padding(5)
                                    .Text(t.Type);
                                table.Cell().Background(bgColor).Padding(5)
                                    .Text(t.Description ?? "-");
                                table.Cell().Background(bgColor).Padding(5)
                                    .Text($"{t.Amount:C}")
                                    .FontColor(t.Type == "Withdrawal" ? Colors.Red.Medium : Colors.Green.Medium);
                                table.Cell().Background(bgColor).Padding(5)
                                    .Text($"{t.BalanceAfterTransaction:C}");
                            }
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span("© ").FontSize(9);
                        txt.Span(DateTime.Now.Year.ToString()).FontSize(9);
                        txt.Span(" MyBank - Confidential").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });

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
