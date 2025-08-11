using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions
{

    public class ExportTransactionsQueryHandler
        : IRequestHandler<ExportTransactionsQuery, ExportTransactionsResultDto>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public ExportTransactionsQueryHandler(
            ITransactionRepository transactionRepository,
            IDistributedCache cache)
        {
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<ExportTransactionsResultDto> Handle(ExportTransactionsQuery request, CancellationToken cancellationToken)
        {
            string cacheKey = $"export:{request.AccountId}:{request.AccountNumber}:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.Format}";

            Log.Information("Attempting to export transactions for Account {AccountNumber} (From: {From}, To: {To}, Format: {Format})",
                request.AccountNumber, request.FromDate, request.ToDate, request.Format);

            // Check Redis cache
            var cachedFile = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cachedFile != null && cachedFile.Length > 0)
            {
                Log.Information("Exported transaction file for {AccountNumber} retrieved from cache", request.AccountNumber);
                return new ExportTransactionsResultDto
                {
                    FileContent = cachedFile,
                    FileName = GetFileName(request),
                    ContentType = GetContentType(request.Format)
                };
            }

            // Fetch transactions
            var transactions = await _transactionRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

            // Apply date filtering
            if (request.FromDate.HasValue)
                transactions = transactions.Where(t => t.Timestamp >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                transactions = transactions.Where(t => t.Timestamp <= request.ToDate.Value);

            if (!transactions.Any())
            {
                Log.Warning("No transactions found for export for Account {AccountNumber}", request.AccountNumber);
                return new ExportTransactionsResultDto();
            }

            // Generate file
            var fileContent = GenerateExportFile(transactions, request.Format);
            var fileName = GetFileName(request);

            // Cache the file
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _cache.SetAsync(cacheKey, fileContent, cacheOptions, cancellationToken);

            Log.Information("Exported transactions for {AccountNumber} and stored in cache", request.AccountNumber);

            return new ExportTransactionsResultDto
            {
                FileContent = fileContent,
                FileName = fileName,
                ContentType = GetContentType(request.Format)
            };
        }

        private byte[] GenerateExportFile(IEnumerable<TransactionHistoryDto> transactions, ExportFormat format)
        {
            // CSV example (Excel and PDF would need specialized libraries)
            if (format == ExportFormat.Csv)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Id,Date,Type,Amount,Description,BalanceAfterTransaction,Status,TargetAccountNumber");
                foreach (var t in transactions)
                {
                    sb.AppendLine($"{t.Id},{t.Timestamp:yyyy-MM-dd HH:mm:ss},{t.Type},{t.Amount},{t.Description},{t.BalanceAfterTransaction},{t.Status},{t.TargetAccountNumber}");
                }
                return Encoding.UTF8.GetBytes(sb.ToString());
            }

            // Placeholder for Excel/PDF export
            return Encoding.UTF8.GetBytes("Export format not implemented yet.");
        }

        private string GetFileName(ExportTransactionsQuery request)
        {
            return $"Transactions_{request.AccountNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.{request.Format.ToString().ToLower()}";
        }

        private string GetContentType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.Csv => "text/csv",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.Pdf => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }


}
