using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Applications.Features.Transactions.DTO;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace BankingAPP.Applications.Features.Transactions.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQueryHandler
        : IRequestHandler<GetTransactionHistoryQuery, IEnumerable<TransactionHistoryDto>>
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IDistributedCache _cache;

        public GetTransactionHistoryQueryHandler(
            ITransactionRepository transactionRepository,
            IDistributedCache cache)
        {
            _transactionRepository = transactionRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<TransactionHistoryDto>> Handle(
            GetTransactionHistoryQuery request,
            CancellationToken cancellationToken)
        {
            string cacheKey = $"transactions:account:{request.AccountId}:page:{request.PageNumber}:size:{request.PageSize}:from:{request.FromDate:yyyyMMdd}:to:{request.ToDate:yyyyMMdd}";

            Log.Information(
                "Fetching transaction history for account {AccountId} (From: {FromDate}, To: {ToDate}) from cache or database",
                request.AccountId, request.FromDate, request.ToDate);

            // Check cache first
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                Log.Information("Transaction history for account {AccountId} retrieved from cache", request.AccountId);
                return JsonSerializer.Deserialize<IEnumerable<TransactionHistoryDto>>(cachedData)
                       ?? Enumerable.Empty<TransactionHistoryDto>();
            }

            // Fetch from repository (date range in database SQL)
            var transactions = await _transactionRepository.GetByAccountIdPagedAsync(
                request.AccountId,
                request.PageNumber,
                request.PageSize,
                request.FromDate,
                request.ToDate,
                cancellationToken
            );

            if (!transactions.Any())
            {
                Log.Warning("No transactions found for account {AccountId} within the given date range", request.AccountId);
                return Enumerable.Empty<TransactionHistoryDto>();
            }

            // Map to DTOs
            var transactionDtos = transactions.Select(t => new TransactionHistoryDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Description = t.Description ?? string.Empty,
                Timestamp = t.Timestamp,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                TargetAccountNumber = t.TargetAccountNumber,
                BalanceAfterTransaction = t.BalanceAfterTransaction
            }).ToList();

            // Cache results
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(transactionDtos),
                cacheOptions,
                cancellationToken
            );

            Log.Information("Transaction history for account {AccountId} cached successfully", request.AccountId);

            return transactionDtos;
        }
    }
}
