using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankingAPP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto, CancellationToken cancellationToken)
        {
            // Optional: get UserId from JWT instead of passing in DTO
            dto.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var accountId = await _accountService.CreateAccountAsync(dto, cancellationToken);
            return Ok(new { AccountId = accountId });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{accountId:guid}")]
        public async Task<IActionResult> GetAccountById(Guid accountId, CancellationToken cancellationToken)
        {
            var account = await _accountService.GetAccountByIdAsync(accountId, cancellationToken);
            if (account == null)
                return NotFound("Account not found");

            return Ok(account);
        }
    }


}
