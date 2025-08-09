using BankingApp.Application.DTO.Accounts;
using BankingApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
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
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                Log.Information("Creating account for UserId={UserId}", userId);

                var response = await _accountService.CreateAccountAsync(userId, dto, cancellationToken);

                Log.Information("Account created successfully. AccountId={AccountId}", response.AccountId);
                return Ok(response); //
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to create account for User");
                return StatusCode(500, "An error occurred while creating the account.");
            }
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{accountId:guid}")]
        public async Task<IActionResult> GetAccountById(Guid accountId, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("Fetching account details. AccountId={AccountId}", accountId);

                var account = await _accountService.GetAccountByIdAsync(accountId, cancellationToken);
                if (account == null)
                {
                    Log.Warning("Account not found. AccountId={AccountId}", accountId);
                    return NotFound("Account not found");
                }

                Log.Information("Account retrieved successfully. AccountId={AccountId}", accountId);
                return Ok(account);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to retrieve account. AccountId={AccountId}", accountId);
                return StatusCode(500, "An error occurred while retrieving the account.");
            }
        }
    }
}
