using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Infrastructure;

namespace BankingAPP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IExportService _exportService;

        public TransactionsController(ITransactionService transactionService, IExportService exportService)
        {
            _transactionService = transactionService;
            _exportService = exportService;

            // For QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var transaction = await _transactionService.DepositAsync(dto);
                return Ok(new
                {
                    Message = "Deposit successful",
                    NewBalance = transaction.BalanceAfterTransaction
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var transaction = await _transactionService.WithdrawAsync(dto);
                return Ok(new
                {
                    Message = "Withdrawal successful",
                    NewBalance = transaction.BalanceAfterTransaction
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InsufficientBalanceException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var transaction = await _transactionService.TransferAsync(dto);
                return Ok(new
                {
                    Message = "Transfer successful",
                    SenderBalance = transaction.BalanceAfterTransaction
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InsufficientBalanceException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("history/{accountId}")]
        public async Task<IActionResult> GetTransactionHistory(
            Guid accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var result = await _transactionService.GetTransactionHistoryByAccountIdAsync(
                accountId, page, pageSize, fromDate, toDate, cancellationToken);

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("export/pdf/{accountId}")]
        public async Task<IActionResult> ExportToPdf(
            Guid accountId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var transactions = await _transactionService.GetTransactionHistoryByAccountIdAsync(
                accountId,
                pageNumber: 1,
                pageSize: int.MaxValue,
                fromDate,
                toDate,
                cancellationToken
            );

            if (transactions == null || !transactions.Items.Any())
                return NotFound("No transactions found for this account.");

            var pdfFile = _exportService.ExportTransactionsToPdf(transactions.Items);
            return File(pdfFile, "application/pdf", $"transactions_{accountId}.pdf");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("export/excel/{accountId}")]
        public async Task<IActionResult> ExportToExcel(
            Guid accountId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var transactions = await _transactionService.GetTransactionHistoryByAccountIdAsync(
                accountId,
                pageNumber: 1,
                pageSize: int.MaxValue,
                fromDate,
                toDate,
                cancellationToken
            );

            if (transactions == null || !transactions.Items.Any())
                return NotFound("No transactions found for this account.");

            var excelFile = _exportService.ExportTransactionsToExcel(transactions.Items);
            return File(
                excelFile,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"transactions_{accountId}.xlsx"
            );
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("accountHistory/{accountNumber}")]
        public async Task<IActionResult> GetTransactionHistoryByAccountNumber(
    string accountNumber,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] DateTime? toDate = null,
    CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var result = await _transactionService.GetAccountHistoryByAccountNumberAsync(
                accountNumber, page, pageSize, fromDate, toDate, cancellationToken);

            if (result == null || !result.Items.Any())
                return NotFound($"No transactions found for account number {accountNumber}.");

            return Ok(result);
        }

    }
}
