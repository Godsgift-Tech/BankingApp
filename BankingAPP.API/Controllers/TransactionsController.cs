using BankingAPP.Applications.Features.Transactions.Commands.Deposit;
using BankingAPP.Applications.Features.Transactions.Commands.Transfer;
using BankingAPP.Applications.Features.Transactions.Commands.Withdraw;
using BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        

        [Authorize(Roles = "Customer")]
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositCommand command, CancellationToken cancellationToken)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.AccountNumber))
                return BadRequest("Account number is required for deposit.");

            if (command.Amount <= 0)
                return BadRequest("Deposit amount must be greater than zero.");

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

   

      

        [Authorize(Roles = "Customer")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawCommand command, CancellationToken cancellationToken)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.AccountNumber))
                return BadRequest("Account number is required for withdrawal.");

            if (command.Amount <= 0)
                return BadRequest("Withdrawal amount must be greater than zero.");

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

       

        [Authorize(Roles = "Customer")]
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferCommand command, CancellationToken cancellationToken)
        {
            if (command == null ||
                string.IsNullOrWhiteSpace(command.FromAccountNumber) ||
                string.IsNullOrWhiteSpace(command.ToAccountNumber))
                return BadRequest("Both source and target account numbers are required for transfer.");

            if (command.Amount <= 0)
                return BadRequest("Transfer amount must be greater than zero.");

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("exportByAccount")]
        public async Task<IActionResult> ExportTransactions(
            [FromQuery] Guid? accountId,
            [FromQuery] string? accountNumber,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] ExportFormat format = ExportFormat.Pdf,
            CancellationToken cancellationToken = default)
        {
            if (accountId == null && string.IsNullOrWhiteSpace(accountNumber))
                return BadRequest("Either accountId or accountNumber must be provided.");

            var query = new ExportTransactionsQuery
            {
                //  NOT fallback to Guid.Empty
                AccountId = accountId,
                AccountNumber = accountNumber,
                FromDate = fromDate,
                ToDate = toDate,
                Format = format
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result.FileContent == null || result.FileContent.Length == 0)
                return NotFound("No transactions found for the specified criteria.");

            return File(result.FileContent, result.ContentType, result.FileName);
        }


    }
}
