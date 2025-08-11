using BankingAPP.Applications.Features.Accounts.DTO;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Queries.GetAccountById
{

    public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto?>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IDistributedCache _cache;

        public GetAccountByIdQueryHandler(IAccountRepository accountRepository, IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _cache = cache;
        }

        public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"account:{request.AccountId}";

            //  Redis Check
            var cachedAccount = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedAccount))
            {
                Log.Information("Returning account from Redis. AccountId: {AccountId}", request.AccountId);
                var account = JsonSerializer.Deserialize<AccountDto>(cachedAccount);
                return account;
            }

            
            Log.Information("Fetching account from DB. AccountId: {AccountId}", request.AccountId);
            var dbAccount = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (dbAccount == null)
            {
                Log.Warning("Account not found. AccountId: {AccountId}", request.AccountId);
                return null;
            }

            var dto = new AccountDto
            {
                Id = dbAccount.Id,
                AccountNumber = dbAccount.AccountNumber,
                Balance = dbAccount.Balance,
                CreatedAt = dbAccount.CreatedAt,

                // Optional: user details
                UserId = dbAccount.UserId,
                FullName = dbAccount.User != null
    ? $"{dbAccount.User.FirstName} {dbAccount.User.LastName}".Trim()
    : string.Empty,


            };



            // 3. Cache it
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
                cancellationToken);

            return dto;
        }
    }


}
