using BankingApp.Application.DTO.Transactions;
using BankingApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        [Authorize]
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


        /// <summary>
        /// Export transactions to PDF.
        /// </summary>
        /// 
        [Authorize(Roles = "Admin")]
        [HttpGet("export/pdf/{accountId}")]
        public async Task<IActionResult> ExportToPdf(
            Guid accountId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var transactions = await _transactionService.GetTransactionHistoryAsync(
                accountId,
                pageNumber: 1,
                pageSize: int.MaxValue,
                fromDate,
                toDate,
                cancellationToken
            );

            var pdfFile = _exportService.ExportTransactionsToPdf(transactions.Items);
            return File(pdfFile, "application/pdf", "transaction-history.pdf");
        }

        /// <summary>
        /// Export transactions to Excel (.xlsx)
        /// </summary>
        [HttpGet("export/excel/{accountId}")]
        public async Task<IActionResult> ExportToExcel(
            Guid accountId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken)
        {
            var transactions = await _transactionService.GetTransactionHistoryAsync(
                accountId,
                pageNumber: 1,
                pageSize: int.MaxValue,
                fromDate,
                toDate,
                cancellationToken
            );

            var excelFile = _exportService.ExportTransactionsToExcel(transactions.Items);
            return File(excelFile,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "transaction-history.xlsx");
        }
    }


}
