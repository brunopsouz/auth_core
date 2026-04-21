using System.Text.Json;
using AuthCore.Domain.Common.DomainEvents;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Security.Emails;
using AuthCore.Infrastructure.Configurations;
using AuthCore.Infrastructure.Services.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthCore.IntegrationTests.Services.Messaging;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenEmailVerificationMessageIsPending_ShouldSendEmailAndMarkProcessed()
    {
        var outboxEvent = new EmailVerificationRequested
        {
            UserId = Guid.NewGuid(),
            Email = "user@example.com",
            Code = "123456",
            RequestedAtUtc = DateTime.UtcNow
        };
        var message = OutboxMessage.Create(
            nameof(EmailVerificationRequested),
            JsonSerializer.Serialize(outboxEvent),
            DateTime.UtcNow);
        var outboxRepository = new FakeOutboxRepository(message);
        var unitOfWork = new SpyUnitOfWork();
        var emailSender = new SpyEmailSender();
        var processor = CreateProcessor(outboxRepository, unitOfWork, emailSender);

        var result = await processor.ProcessPendingAsync();

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(emailSender.SentVerifications);
        Assert.Equal(("user@example.com", "123456"), emailSender.SentVerifications[0]);
        Assert.Single(outboxRepository.UpdatedMessages);
        Assert.NotNull(outboxRepository.UpdatedMessages[0].ProcessedAtUtc);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenMessageTypeIsUnknown_ShouldRegisterFailureAndCommit()
    {
        var message = OutboxMessage.Create(
            "UnknownMessage",
            "{}",
            DateTime.UtcNow);
        var outboxRepository = new FakeOutboxRepository(message);
        var unitOfWork = new SpyUnitOfWork();
        var emailSender = new SpyEmailSender();
        var processor = CreateProcessor(outboxRepository, unitOfWork, emailSender);

        var result = await processor.ProcessPendingAsync();

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Empty(emailSender.SentVerifications);
        Assert.Single(outboxRepository.UpdatedMessages);
        Assert.Equal(1, outboxRepository.UpdatedMessages[0].AttemptCount);
        Assert.Contains("não suportado", outboxRepository.UpdatedMessages[0].LastError);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenEmailSenderFails_ShouldRegisterFailureAndCommit()
    {
        var outboxEvent = new EmailVerificationRequested
        {
            UserId = Guid.NewGuid(),
            Email = "user@example.com",
            Code = "123456",
            RequestedAtUtc = DateTime.UtcNow
        };
        var message = OutboxMessage.Create(
            nameof(EmailVerificationRequested),
            JsonSerializer.Serialize(outboxEvent),
            DateTime.UtcNow);
        var outboxRepository = new FakeOutboxRepository(message);
        var unitOfWork = new SpyUnitOfWork();
        var emailSender = new SpyEmailSender
        {
            ExceptionToThrow = new InvalidOperationException("SMTP indisponível.")
        };
        var processor = CreateProcessor(outboxRepository, unitOfWork, emailSender);

        var result = await processor.ProcessPendingAsync();

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Single(outboxRepository.UpdatedMessages);
        Assert.Equal(1, outboxRepository.UpdatedMessages[0].AttemptCount);
        Assert.Equal("SMTP indisponível.", outboxRepository.UpdatedMessages[0].LastError);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);
    }

    private static OutboxProcessor CreateProcessor(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IEmailSender emailSender)
    {
        return new OutboxProcessor(
            outboxRepository,
            unitOfWork,
            emailSender,
            Options.Create(new OutboxOptions
            {
                BatchSize = 20,
                MaxAttempts = 5
            }),
            new OutboxMetrics(),
            NullLogger<OutboxProcessor>.Instance);
    }

    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        private readonly List<OutboxMessage> _messages;

        public FakeOutboxRepository(params OutboxMessage[] messages)
        {
            _messages = [.. messages];
        }

        public List<OutboxMessage> UpdatedMessages { get; } = [];

        public Task AddAsync(OutboxMessage message)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int take, int maxAttempts)
        {
            IReadOnlyCollection<OutboxMessage> messages = _messages
                .Where(message => message.ProcessedAtUtc is null && message.AttemptCount < maxAttempts)
                .Take(take)
                .ToArray();

            return Task.FromResult(messages);
        }

        public Task UpdateAsync(OutboxMessage message)
        {
            UpdatedMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class SpyUnitOfWork : IUnitOfWork
    {
        public int BeginCount { get; private set; }

        public int CommitCount { get; private set; }

        public int RollbackCount { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginCount++;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCount++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class SpyEmailSender : IEmailSender
    {
        public List<(string Email, string Code)> SentVerifications { get; } = [];

        public Exception? ExceptionToThrow { get; init; }

        public Task SendEmailVerificationAsync(
            string email,
            string code,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            SentVerifications.Add((email, code));
            return Task.CompletedTask;
        }
    }
}
