using BankingAPP.Applications.Features.Accounts.Commands.CreateAccount;
using BankingAPP.Applications.Features.Accounts.Commands.DeleteAccount;
using BankingAPP.Applications.Features.Accounts.Commands.UpdateAccount;
using BankingAPP.Applications.Features.Accounts.Queries.GetAccountById;
using BankingAPP.Applications.Features.Accounts.Queries.GetAllAcounts;
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
        [HttpGet("allAccounts")]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAllAccountsQuery(), cancellationToken);
            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateAccountCommand command, CancellationToken cancellationToken)
        {
            if (command == null)
                return BadRequest("Account creation data is required.");

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (BankingAPP.Applications.Features.Common.Exceptions.ValidationException ex)
            {
                return BadRequest(new { error = ex.Message});
            }
        }


        [Authorize(Roles = "Customer,Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountCommand command, CancellationToken cancellationToken)
        {
            if (id != command.AccountId)
                return BadRequest("The provided ID does not match the account ID.");

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
