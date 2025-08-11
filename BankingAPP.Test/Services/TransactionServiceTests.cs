//using BankingApp.Application.DTO.Common;
//using BankingApp.Application.DTO.Transactions;
//using BankingApp.Application.Interfaces.Repository;
//using BankingApp.Application.Services;
//using BankingApp.Core.Entities;
//using BankingApp.Core.Enums;
//using FluentAssertions;
//using Microsoft.Extensions.Caching.Distributed;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;

//namespace BankingAPP.Test.Services
//{
//    public class TransactionServiceTests
//    {
//        private readonly Mock<ITransactionRepository> _transactionRepoMock;
//        private readonly Mock<IDistributedCache> _cacheMock;
//        private readonly TransactionService _transactionService;

//        public TransactionServiceTests()
//        {
//            _transactionRepoMock = new Mock<ITransactionRepository>();
//            _cacheMock = new Mock<IDistributedCache>();

//            _transactionService = new TransactionService(
//                _transactionRepoMock.Object,
//                _cacheMock.Object
//            );
//        }

//        private byte[] SerializeToBytes<T>(T obj)
//        {
//            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
//        }

//        [Fact]
//        public async Task DepositAsync_ShouldIncreaseBalance_AndReturnTransaction()
//        {
//            var accountId = Guid.NewGuid();
//            var account = new Account { Id = accountId, Balance = 1000 };

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(accountId))
//                .ReturnsAsync(account);

//            var dto = new DepositDto
//            {
//                AccountId = accountId,
//                Amount = 500,
//                Description = "Test deposit"
//            };

//            var result = await _transactionService.DepositAsync(dto);

//            result.Should().NotBeNull();
//            result.Type.Should().Be(TransactionType.Deposit);
//            result.Amount.Should().Be(500);
//            account.Balance.Should().Be(1500);

//            _transactionRepoMock.Verify(r => r.UpdateAccountAsync(account), Times.Once);
//            _transactionRepoMock.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Once);
//            _transactionRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
//        }

//        [Fact]
//        public async Task WithdrawAsync_ShouldThrow_WhenBalanceIsInsufficient()
//        {
//            var accountId = Guid.NewGuid();
//            var account = new Account { Id = accountId, Balance = 100 };

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(accountId))
//                .ReturnsAsync(account);

//            var dto = new WithdrawDto
//            {
//                AccountId = accountId,
//                Amount = 500,
//                Description = "Test withdrawal"
//            };

//            var act = async () => await _transactionService.WithdrawAsync(dto);

//            await act.Should().ThrowAsync<InsufficientBalanceException>();
//        }

//        [Fact]
//        public async Task WithdrawAsync_ShouldDecreaseBalance_WhenSufficientFunds()
//        {
//            var accountId = Guid.NewGuid();
//            var account = new Account { Id = accountId, Balance = 1000 };

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(accountId))
//                .ReturnsAsync(account);

//            var dto = new WithdrawDto
//            {
//                AccountId = accountId,
//                Amount = 200,
//                Description = "Test withdrawal"
//            };

//            var result = await _transactionService.WithdrawAsync(dto);

//            result.Should().NotBeNull();
//            result.Type.Should().Be(TransactionType.Withdrawal);
//            result.Amount.Should().Be(200);
//            account.Balance.Should().Be(800);

//            _transactionRepoMock.Verify(r => r.UpdateAccountAsync(account), Times.Once);
//            _transactionRepoMock.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Once);
//            _transactionRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
//        }

//        [Fact]
//        public async Task TransferAsync_ShouldThrow_WhenAccountsNotFound()
//        {
//            var fromId = Guid.NewGuid();

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(fromId))
//                .ReturnsAsync((Account)null);

//            var dto = new TransferDto
//            {
//                FromAccountId = fromId,
//                ToAccountNumber = "1234567890",
//                Amount = 100,
//                Description = "Test transfer"
//            };

//            var act = async () => await _transactionService.TransferAsync(dto);

//            await act.Should().ThrowAsync<NotFoundException>();
//        }

//        [Fact]
//        public async Task TransferAsync_ShouldThrow_WhenInsufficientFunds()
//        {
//            var fromId = Guid.NewGuid();
//            var fromAccount = new Account { Id = fromId, Balance = 50 };
//            var toAccount = new Account { Id = Guid.NewGuid(), Balance = 1000, AccountNumber = "1234567890" };

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(fromId))
//                .ReturnsAsync(fromAccount);

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByNumberAsync("1234567890"))
//                .ReturnsAsync(toAccount);

//            var dto = new TransferDto
//            {
//                FromAccountId = fromId,
//                ToAccountNumber = "1234567890",
//                Amount = 500,
//                Description = "Test transfer"
//            };

//            var act = async () => await _transactionService.TransferAsync(dto);

//            await act.Should().ThrowAsync<InsufficientBalanceException>();
//        }

//        [Fact]
//        public async Task TransferAsync_ShouldMoveFunds_WhenSufficientBalance()
//        {
//            var fromId = Guid.NewGuid();
//            var fromAccount = new Account { Id = fromId, Balance = 1000, AccountNumber = "111111" };
//            var toAccount = new Account { Id = Guid.NewGuid(), Balance = 500, AccountNumber = "1234567890" };

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByIdAsync(fromId))
//                .ReturnsAsync(fromAccount);

//            _transactionRepoMock
//                .Setup(r => r.GetAccountByNumberAsync("1234567890"))
//                .ReturnsAsync(toAccount);

//            var dto = new TransferDto
//            {
//                FromAccountId = fromId,
//                ToAccountNumber = "1234567890",
//                Amount = 300,
//                Description = "Test transfer"
//            };

//            var result = await _transactionService.TransferAsync(dto);

//            result.Should().NotBeNull();
//            result.Type.Should().Be(TransactionType.Transfer);
//            result.Amount.Should().Be(300);
//            fromAccount.Balance.Should().Be(700);
//            toAccount.Balance.Should().Be(800);

//            _transactionRepoMock.Verify(r => r.UpdateAccountAsync(fromAccount), Times.Once);
//            _transactionRepoMock.Verify(r => r.UpdateAccountAsync(toAccount), Times.Once);
//            _transactionRepoMock.Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Exactly(2));
//            _transactionRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
//        }

//        [Fact]
//        public async Task GetTransactionHistory_ShouldReturnFromCache_WhenCacheExists()
//        {
//            var accountId = Guid.NewGuid();
//            var cachedData = new PagedResult<TransactionHistoryDto>
//            {
//                Page = 1,
//                PageSize = 10,
//                TotalItems = 1,
//                Items = new List<TransactionHistoryDto>
//                {
//                    new TransactionHistoryDto
//                    {
//                        Id = Guid.NewGuid(),
//                        Amount = 500,
//                        Description = "Cached transaction",
//                        Type = "Deposit",
//                        Status = "Success",
//                        Timestamp = DateTime.UtcNow
//                    }
//                }
//            };

//            _cacheMock
//                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync(SerializeToBytes(cachedData));

//            var result = await _transactionService.GetTransactionHistoryByAccountIdAsync(
//                accountId, 1, 10, null, null, CancellationToken.None);

//            result.Should().NotBeNull();
//            result.Items.Should().HaveCount(1);
//            result.Items[0].Description.Should().Be("Cached transaction");

//            _transactionRepoMock.Verify(r => r.GetPagedTransactionsByAccountIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), null, null, It.IsAny<CancellationToken>()), Times.Never);
//        }

//        [Fact]
//        public async Task GetTransactionHistory_ShouldFetchFromDb_AndCacheResult_WhenCacheIsEmpty()
//        {
//            var accountId = Guid.NewGuid();

//            _cacheMock
//                .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
//                .ReturnsAsync((byte[])null);

//            var transactions = new List<Transaction>
//            {
//                new Transaction
//                {
//                    Id = Guid.NewGuid(),
//                    Amount = 200,
//                    Description = "DB transaction",
//                    Type = TransactionType.Deposit,
//                    Status = TransactionStatus.Success,
//                    Timestamp = DateTime.UtcNow
//                }
//            };

//            _transactionRepoMock
//                .Setup(r => r.GetPagedTransactionsByAccountIdAsync(accountId, 1, 10, null, null, It.IsAny<CancellationToken>()))
//                .ReturnsAsync((transactions, transactions.Count));

//            _cacheMock
//                .Setup(c => c.SetAsync(
//                    It.IsAny<string>(),
//                    It.IsAny<byte[]>(),
//                    It.IsAny<DistributedCacheEntryOptions>(),
//                    It.IsAny<CancellationToken>()))
//                .Returns(Task.CompletedTask);

//            var result = await _transactionService.GetTransactionHistoryByAccountIdAsync(
//                accountId, 1, 10, null, null, CancellationToken.None);

//            result.Should().NotBeNull();
//            result.Items.Should().HaveCount(1);
//            result.Items[0].Description.Should().Be("DB transaction");

//            _cacheMock.Verify(c => c.SetAsync(
//                It.IsAny<string>(),
//                It.IsAny<byte[]>(),
//                It.IsAny<DistributedCacheEntryOptions>(),
//                It.IsAny<CancellationToken>()), Times.Once);
//        }
//    }
//}
