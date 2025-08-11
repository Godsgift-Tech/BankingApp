

using BankingAPP.Applications.Features.Transactions.Commands.Deposit;
using BankingAPP.Applications.Features.Transactions.Commands.Transfer;
using BankingAPP.Applications.Features.Transactions.Commands.Withdraw;
using BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TransactionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Deposit funds into an account.
        /// </summary>
        [Authorize(Roles = "Customer,Admin")]
        /// 
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositCommand command)
        {
            if (command == null)
                return BadRequest("Invalid request payload.");

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Withdraw funds from an account.
        /// </summary>
        [Authorize(Roles = "Customer,Admin")]

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawCommand command)
        {
            if (command == null)
                return BadRequest("Invalid request payload.");

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Transfer funds from one account to another.
        /// </summary>
        /// 
        [Authorize(Roles = "Customer,Admin")]

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferCommand command)
        {
            if (command == null)
                return BadRequest("Invalid request payload.");

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Export transactions for a given account and date range.
        /// </summary>
        [Authorize(Roles = "Admin")]

        [HttpGet("exportByAccountId")]
        public async Task<IActionResult> ExportTransactions(
            [FromQuery] Guid accountId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ExportFormat format = ExportFormat.Pdf)
        {
            var query = new ExportTransactionsQuery
            {
                AccountId = accountId,
                FromDate = fromDate,
                ToDate = toDate,
                Format = format
            };

            var result = await _mediator.Send(query);

            if (result.FileContent == null || result.FileContent.Length == 0)
            {
                return NotFound("No transactions found for the specified criteria.");
            }

            return File(result.FileContent, result.ContentType, result.FileName);
        }
    }


}
