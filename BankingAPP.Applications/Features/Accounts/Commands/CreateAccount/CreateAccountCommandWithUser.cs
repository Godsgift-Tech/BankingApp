using BankingAPP.Applications.Features.Accounts.DTO;
using MediatR;

namespace BankingAPP.Applications.Features.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandWithUser : IRequest<AccountDto>
    {
        public string UserId { get; set; } = default!;
        public string AccountType { get; set; } = default!;
        public string Currency { get; set; } = "NGN";
    }
}
