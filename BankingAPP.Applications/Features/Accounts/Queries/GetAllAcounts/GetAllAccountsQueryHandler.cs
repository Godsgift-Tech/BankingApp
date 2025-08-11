using BankingAPP.Applications.Features.Accounts.DTO;
using BankingAPP.Applications.Features.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Queries.GetAllAcounts
{
    public class GetAllAccountsQueryHandler : IRequestHandler<GetAllAccountsQuery, IPagedList<AccountDto>>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IDistributedCache _cache;

        public GetAllAccountsQueryHandler(IAccountRepository accountRepository, IDistributedCache cache)
        {
            _accountRepository = accountRepository;
            _cache = cache;
        }

        public async Task<IPagedList<AccountDto>> Handle(GetAllAccountsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"accounts:all:p{request.PageNumber}:s{request.PageSize}";

            //  Try to get from Redis
            var cachedAccounts = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedAccounts))
            {
                return JsonSerializer.Deserialize<PagedList<AccountDto>>(cachedAccounts) ??
                       new PagedList<AccountDto>(new List<AccountDto>(), request.PageNumber, request.PageSize);
            }

            // Picking from cache before we get to dabase incase cache is empty
            var accounts = await _accountRepository.GetAllAsync(cancellationToken);

            var dtos = accounts.Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                AccountName = $"{a.User.FirstName} {a.User.LastName}",
                Balance = a.Balance,
                CreatedAt = a.CreatedAt,
                UserId = a.UserId,
                FullName = $"{a.User.FirstName} {a.User.LastName}"
            }).ToPagedList(request.PageNumber, request.PageSize);

            // Saving by Redis
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(dtos),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                cancellationToken);

            return dtos;
        }
    }
}
