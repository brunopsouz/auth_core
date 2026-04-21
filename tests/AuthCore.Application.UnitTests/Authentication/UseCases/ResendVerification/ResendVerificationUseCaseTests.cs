using System.Text.Json;
using global::AuthCore.Application.UnitTests.Authentication.Support;
using AuthCore.Application.Authentication.UseCases.ResendVerification;
using AuthCore.Domain.Common.DomainEvents;

namespace AuthCore.Application.UnitTests.Authentication.UseCases.ResendVerification;

public sealed class ResendVerificationUseCaseTests
{
    [Fact]
    public async Task Execute_WhenUserIsPendingAndCooldownExpired_ShouldUpdateVerificationAndPersistOutbox()
    {
        var userReadRepository = new FakeUserReadRepository();
        var emailVerificationRepository = new FakeEmailVerificationRepository();
        var emailVerificationService = new FakeEmailVerificationService
        {
            Material = new()
            {
                Code = "777777",
                Hash = "777777-hash"
            }
        };
        var outboxRepository = new FakeOutboxRepository();
        var unitOfWork = new SpyUnitOfWork();
        var user = AuthenticationFixtures.CreateUnverifiedUser();
        var existingVerification = AuthCore.Domain.Passports.Aggregates.EmailVerification.Issue(
            user.Id,
            user.Email.Value,
            "old-hash",
            DateTime.UtcNow.AddMinutes(5),
            5,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(-2));
        var useCase = new ResendVerificationUseCase(
            userReadRepository,
            emailVerificationRepository,
            emailVerificationService,
            outboxRepository,
            unitOfWork);

        userReadRepository.Store(user);
        emailVerificationRepository.Store(existingVerification);

        await useCase.Execute(new ResendVerificationCommand
        {
            Email = user.Email.Value
        });

        var updatedVerification = Assert.Single(emailVerificationRepository.UpdatedVerifications);
        var outboxMessage = Assert.Single(outboxRepository.AddedMessages);
        var outboxEvent = JsonSerializer.Deserialize<EmailVerificationRequested>(outboxMessage.Content);

        Assert.Equal(emailVerificationService.Material.Hash, updatedVerification.CodeHash);
        Assert.Equal(user.Id, updatedVerification.UserId);
        Assert.NotNull(outboxEvent);
        Assert.Equal(user.Id, outboxEvent!.UserId);
        Assert.Equal(emailVerificationService.Material.Code, outboxEvent.Code);
        Assert.Equal(1, unitOfWork.BegunTransactions);
        Assert.Equal(1, unitOfWork.CommittedTransactions);
        Assert.Equal(0, unitOfWork.RolledBackTransactions);
    }

    [Fact]
    public async Task Execute_WhenVerificationIsInCooldown_ShouldCompleteWithoutPersistingChanges()
    {
        var userReadRepository = new FakeUserReadRepository();
        var emailVerificationRepository = new FakeEmailVerificationRepository();
        var emailVerificationService = new FakeEmailVerificationService();
        var outboxRepository = new FakeOutboxRepository();
        var unitOfWork = new SpyUnitOfWork();
        var user = AuthenticationFixtures.CreateUnverifiedUser();
        var existingVerification = AuthCore.Domain.Passports.Aggregates.EmailVerification.Issue(
            user.Id,
            user.Email.Value,
            "old-hash",
            DateTime.UtcNow.AddMinutes(5),
            5,
            DateTime.UtcNow.AddMinutes(2),
            DateTime.UtcNow);
        var useCase = new ResendVerificationUseCase(
            userReadRepository,
            emailVerificationRepository,
            emailVerificationService,
            outboxRepository,
            unitOfWork);

        userReadRepository.Store(user);
        emailVerificationRepository.Store(existingVerification);

        await useCase.Execute(new ResendVerificationCommand
        {
            Email = user.Email.Value
        });

        Assert.Empty(emailVerificationRepository.AddedVerifications);
        Assert.Empty(emailVerificationRepository.UpdatedVerifications);
        Assert.Empty(outboxRepository.AddedMessages);
        Assert.Equal(1, unitOfWork.BegunTransactions);
        Assert.Equal(1, unitOfWork.CommittedTransactions);
        Assert.Equal(0, unitOfWork.RolledBackTransactions);
    }
}
