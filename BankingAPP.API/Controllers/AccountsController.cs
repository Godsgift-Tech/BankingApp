using BankingAPP.Applications.Features.Accounts.Commands.CreateAccount;
using BankingAPP.Applications.Features.Accounts.Commands.DeleteAccount;
using BankingAPP.Applications.Features.Accounts.Commands.UpdateAccount;
using BankingAPP.Applications.Features.Accounts.Queries.GetAccountById;
using BankingAPP.Applications.Features.Accounts.Queries.GetAllAcounts;
using BankingAPP.Applications.Features.Transactions.Commands.Deposit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAccountByIdQuery(id), cancellationToken);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllAccountsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPost("{accountId:guid}/deposit")]
        public async Task<IActionResult> Deposit(Guid accountId, [FromBody] DepositCommand command, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (accountId != command.AccountId)
                return BadRequest("AccountId in URL does not match AccountId in body.");

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountCommand command, CancellationToken cancellationToken)
        {
            if (id != command.AccountId)
                return BadRequest("validate the two IDs.");

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteAccountCommand(id), cancellationToken);
            return NoContent();
        }
    }
}
