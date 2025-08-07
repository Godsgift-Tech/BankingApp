using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]



    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositDto dto, CancellationToken cancellationToken)
        {
            await _transactionService.DepositAsync(dto);
            return Ok("Deposit successful");
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawDto dto, CancellationToken cancellationToken)
        {
            await _transactionService.WithdrawAsync(dto);
            return Ok("Withdrawal successful");
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto, CancellationToken cancellationToken)
        {
            await _transactionService.TransferAsync(dto);
            return Ok("Transfer successful");
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetTransactionHistory(
      Guid accountId,
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] DateTime? fromDate = null,
      [FromQuery] DateTime? toDate = null,
      CancellationToken cancellationToken = default)
        {
            var result = await _transactionService.GetTransactionHistoryAsync(
                accountId, page, pageSize, fromDate, toDate, cancellationToken);

            return Ok(result);
        }

    }


}
