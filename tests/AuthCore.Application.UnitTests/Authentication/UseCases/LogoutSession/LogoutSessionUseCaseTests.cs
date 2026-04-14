using global::AuthCore.Application.UnitTests.Authentication.Support;
using AuthCore.Application.Authentication.UseCases.LogoutSession;
using AuthCore.Domain.Passports.Aggregates;

namespace AuthCore.Application.UnitTests.Authentication.UseCases.LogoutSession;

public sealed class LogoutSessionUseCaseTests
{
    [Fact]
    public async Task Execute_WhenRefreshTokenIsActive_ShouldRevokeTokenTransactionally()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var refreshTokenService = new FakeRefreshTokenService();
        var unitOfWork = new SpyUnitOfWork();
        var rawRefreshToken = "active-refresh-token";
        var refreshToken = RefreshToken.IssueInitial(
            Guid.NewGuid(),
            refreshTokenService.ComputeHash(rawRefreshToken),
            DateTime.UtcNow.AddDays(3));
        var useCase = new LogoutSessionUseCase(
            refreshTokenRepository,
            refreshTokenService,
            unitOfWork);

        refreshTokenRepository.Store(refreshToken);

        await useCase.Execute(new LogoutSessionCommand
        {
            RefreshToken = rawRefreshToken
        });

        var updatedRefreshToken = Assert.Single(refreshTokenRepository.UpdatedRefreshTokens);

        Assert.Equal(refreshToken.Id, updatedRefreshToken.Id);
        Assert.NotNull(updatedRefreshToken.RevokedAtUtc);
        Assert.Equal("logout", updatedRefreshToken.RevocationReason);
        Assert.Empty(refreshTokenRepository.RevokeFamilyCalls);
        Assert.Equal(1, unitOfWork.BegunTransactions);
        Assert.Equal(1, unitOfWork.CommittedTransactions);
        Assert.Equal(0, unitOfWork.RolledBackTransactions);
    }

    [Fact]
    public async Task Execute_WhenRefreshTokenWasAlreadyConsumed_ShouldRevokeFamilyTransactionally()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var refreshTokenService = new FakeRefreshTokenService();
        var unitOfWork = new SpyUnitOfWork();
        var rawRefreshToken = "consumed-refresh-token";
        var consumedRefreshToken = RefreshToken.IssueInitial(
                Guid.NewGuid(),
                refreshTokenService.ComputeHash(rawRefreshToken),
                DateTime.UtcNow.AddDays(3))
            .Consume(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(-10));
        var useCase = new LogoutSessionUseCase(
            refreshTokenRepository,
            refreshTokenService,
            unitOfWork);

        refreshTokenRepository.Store(consumedRefreshToken);

        await useCase.Execute(new LogoutSessionCommand
        {
            RefreshToken = rawRefreshToken
        });

        var revokeFamilyCall = Assert.Single(refreshTokenRepository.RevokeFamilyCalls);

        Assert.Equal(consumedRefreshToken.FamilyId, revokeFamilyCall.FamilyId);
        Assert.Equal("logout", revokeFamilyCall.Reason);
        Assert.Empty(refreshTokenRepository.UpdatedRefreshTokens);
        Assert.Equal(1, unitOfWork.BegunTransactions);
        Assert.Equal(1, unitOfWork.CommittedTransactions);
        Assert.Equal(0, unitOfWork.RolledBackTransactions);
    }

    [Fact]
    public async Task Execute_WhenRefreshTokenDoesNotExist_ShouldCompleteWithoutPersistingChanges()
    {
        var refreshTokenRepository = new FakeRefreshTokenRepository();
        var refreshTokenService = new FakeRefreshTokenService();
        var unitOfWork = new SpyUnitOfWork();
        var useCase = new LogoutSessionUseCase(
            refreshTokenRepository,
            refreshTokenService,
            unitOfWork);

        await useCase.Execute(new LogoutSessionCommand
        {
            RefreshToken = "missing-refresh-token"
        });

        Assert.Empty(refreshTokenRepository.UpdatedRefreshTokens);
        Assert.Empty(refreshTokenRepository.RevokeFamilyCalls);
        Assert.Equal(0, unitOfWork.BegunTransactions);
        Assert.Equal(0, unitOfWork.CommittedTransactions);
        Assert.Equal(0, unitOfWork.RolledBackTransactions);
    }
}
