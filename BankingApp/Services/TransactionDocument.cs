using BankingApp.Application.DTO.Transactions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingApp.Application.Services
{

    public class TransactionDocument : IDocument
    {
        private readonly List<TransactionHistoryDto> _transactions;

        public TransactionDocument(List<TransactionHistoryDto> transactions)
        {
            _transactions = transactions;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4); // Optional

                page.Header().Text("Transaction History")
                    .FontSize(20)
                    .Bold()
                    .AlignCenter();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(); // Date
                        columns.RelativeColumn(); // Amount
                        columns.RelativeColumn(); // Type
                        columns.RelativeColumn(); // Description
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        header.Cell().Text("Date").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Type").Bold();
                        header.Cell().Text("Description").Bold();
                    });

                    // Table Rows
                    foreach (var tx in _transactions)
                    {
                        table.Cell().Text(tx.Timestamp.ToString("yyyy-MM-dd"));
                        table.Cell().Text(tx.Amount.ToString("N2"));
                        table.Cell().Text(tx.Type.ToString());
                        table.Cell().Text(tx.Description ?? "-");
                    }
                });

                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            });
        }
    }


}
