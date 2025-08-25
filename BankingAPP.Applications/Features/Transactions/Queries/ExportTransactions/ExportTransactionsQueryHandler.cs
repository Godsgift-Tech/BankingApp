using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions;
using MediatR;
using Serilog;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BankingAPP.Applications.Features.Transactions.Handlers
{
    public class ExportTransactionsQueryHandler
        : IRequestHandler<ExportTransactionsQuery, ExportTransactionsResultDto>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IExportService _exportService;
        private readonly IDatabase _redisCache;

        public ExportTransactionsQueryHandler(
            ITransactionRepository transactionRepository,
            IExportService exportService,
            IDatabase redisCache)
        {
            _transactionRepository = transactionRepository;
            _exportService = exportService;
            _redisCache = redisCache;
        }

        public async Task<ExportTransactionsResultDto> Handle(
            ExportTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            // Resolve AccountId if not provided but AccountNumber is
            if (!request.AccountId.HasValue && !string.IsNullOrEmpty(request.AccountNumber))
            {
                var getaccount = await _transactionRepository.GetAccountByNumberAsync(request.AccountNumber, cancellationToken);
                if (getaccount == null)
                {
                    Log.Warning("Account with number {AccountNumber} not found.", request.AccountNumber);
                    return new ExportTransactionsResultDto
                    {
                        FileContent = Array.Empty<byte>(),
                        ContentType = "application/octet-stream",
                        FileName = "NoAccountFound.txt"
                    };
                }

                request.AccountId = getaccount.Id;
            }

            // Ensure AccountId is available
            if (!request.AccountId.HasValue)
            {
                throw new ArgumentException("Either AccountId or AccountNumber must be provided.");
            }

            var cacheKey = $"transactions:{request.AccountId}:{request.FromDate:yyyyMMdd}:{request.ToDate:yyyyMMdd}:{request.Format}";

            // Try Redis cache first
            var cachedData = await _redisCache.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
                Log.Information("Cache hit for export transactions {CacheKey}", cacheKey);
                return JsonSerializer.Deserialize<ExportTransactionsResultDto>(cachedData!)!;
            }

            // Get transactions via repository
            var transactions = await _transactionRepository.GetTransactionsAsync(
                request.AccountId.Value,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            if (!transactions.Any())
            {
                Log.Warning("No transactions found for AccountId {AccountId}", request.AccountId.Value);
                return new ExportTransactionsResultDto
                {
                    FileContent = Array.Empty<byte>(),
                    ContentType = "application/octet-stream",
                    FileName = "NoTransactionsFound.txt"
                };
            }

            //  Get account to fetch Currency
            var account = await _transactionRepository.GetByAccountIdAsync(request.AccountId.Value, cancellationToken);
            if (account == null)
            {
                throw new ArgumentException("Account not found.");
            }

            //  Map to DTOs so AmountWithCurrency and BalanceAfterTransactionWithCurrency are used
            var transactionDtos = transactions.Select(t => new TransactionHistoryDto
            {
                Id = t.Id,
                AccountNumber = account.AccountNumber,  
                Currency = string.IsNullOrWhiteSpace(account.Currency) ? "NGN" : account.Currency,
                Amount = t.Amount,
                Description = t.Description ?? string.Empty,
                Timestamp = t.Timestamp,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                TargetAccountNumber = t.TargetAccountNumber,
                BalanceAfterTransaction = t.BalanceAfterTransaction
            }).ToList();

            // Export file with DTOs (not domain entities)
            var (fileContent, contentType, fileName) = request.Format switch
            {
                ExportFormat.Pdf => (_exportService.ExportTransactionsToPdf(transactionDtos), "application/pdf", "Transactions.pdf"),
                ExportFormat.Excel => (_exportService.ExportTransactionsToExcel(transactionDtos), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Transactions.xlsx"),
                _ => throw new ArgumentException("Unsupported export format")
            };

            var result = new ExportTransactionsResultDto
            {
                FileContent = fileContent,
                ContentType = contentType,
                FileName = fileName
            };

            // Cache result
            await _redisCache.StringSetAsync(cacheKey, JsonSerializer.Serialize(result), TimeSpan.FromMinutes(10));

            Log.Information("Transactions export generated and cached for AccountId {AccountId}", request.AccountId.Value);

            return result;
        }
    }
}
