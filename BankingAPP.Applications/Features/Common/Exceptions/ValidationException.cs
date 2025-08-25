namespace BankingAPP.Applications.Features.Common.Exceptions
{
    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(IEnumerable<string> errors)
            : base("One or more validation failures occurred.")
        {
            Errors = errors.ToList();
        }

        public ValidationException(string message)
            : base(message)
        {
            Errors = new List<string> { message };
        }
    }
}
