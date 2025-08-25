using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Common.Exceptions
{
    public class BusinessRuleValidationException : Exception
    {
        // to catch errors with account type duplication
        public BusinessRuleValidationException(string message)
            : base(message)
        { }
    }
}
