using AuthCore.Domain.Common.Enums;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Aggregates;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Security.Cryptography;
using AuthCore.Domain.Security.Tokens.Models;
using AuthCore.Domain.Security.Tokens.Services;
using AuthCore.Domain.Users.Aggregates;
using AuthCore.Domain.Users.Enums;
using AuthCore.Domain.Users.Repositories;

namespace AuthCore.Application.UnitTests.Authentication.Support;

internal sealed class FakeUserReadRepository : IUserReadRepository
{
    private readonly Dictionary<Guid, User> _usersById = [];
    private readonly Dictionary<string, User> _usersByEmail = [];
    private readonly Dictionary<Guid, User> _usersByIdentifier = [];

    public Task<User?> GetByIdAsync(Guid userId)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByUserIdentifierAsync(Guid userIdentifier)
    {
        _usersByIdentifier.TryGetValue(userIdentifier, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        _usersByEmail.TryGetValue(email.Trim().ToLowerInvariant(), out var user);
        return Task.FromResult(user);
    }

    public void Store(User user)
    {
        _usersById[user.Id] = user;
        _usersByEmail[user.Email.Value] = user;
        _usersByIdentifier[user.UserIdentifier] = user;
    }
}

internal sealed class FakePasswordRepository : IPasswordRepository
{
    private readonly Dictionary<Guid, Password> _passwordsByUserId = [];

    public List<Password> AddedPasswords { get; } = [];

    public List<Password> UpdatedPasswords { get; } = [];

    public Task AddAsync(Password password)
    {
        AddedPasswords.Add(password);
        _passwordsByUserId[password.UserId] = password;
        return Task.CompletedTask;
    }

    public Task<Password?> GetByUserIdAsync(Guid userId)
    {
        _passwordsByUserId.TryGetValue(userId, out var password);
        return Task.FromResult(password);
    }

    public Task UpdateAsync(Password password)
    {
        UpdatedPasswords.Add(password);
        _passwordsByUserId[password.UserId] = password;
        return Task.CompletedTask;
    }

    public void Store(Password password)
    {
        _passwordsByUserId[password.UserId] = password;
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _usersById = [];

    public List<User> AddedUsers { get; } = [];

    public List<User> UpdatedUsers { get; } = [];

    public List<User> DeletedUsers { get; } = [];

    public Task AddAsync(User user)
    {
        AddedUsers.Add(user);
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user)
    {
        UpdatedUsers.Add(user);
        _usersById[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user)
    {
        DeletedUsers.Add(user);
        _usersById.Remove(user.Id);
        return Task.CompletedTask;
    }

    public void Store(User user)
    {
        _usersById[user.Id] = user;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, RefreshToken> _refreshTokensByHash = [];

    public List<RefreshToken> AddedRefreshTokens { get; } = [];

    public List<RefreshToken> UpdatedRefreshTokens { get; } = [];

    public List<(Guid FamilyId, DateTime RevokedAtUtc, string Reason)> RevokeFamilyCalls { get; } = [];

    public List<(Guid UserId, DateTime RevokedAtUtc, string Reason)> RevokeUserCalls { get; } = [];

    public Task AddAsync(RefreshToken refreshToken)
    {
        AddedRefreshTokens.Add(refreshToken);
        _refreshTokensByHash[refreshToken.TokenHash] = refreshToken;
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        _refreshTokensByHash.TryGetValue(tokenHash.Trim(), out var refreshToken);
        return Task.FromResult(refreshToken);
    }

    public Task UpdateAsync(RefreshToken refreshToken)
    {
        UpdatedRefreshTokens.Add(refreshToken);
        _refreshTokensByHash[refreshToken.TokenHash] = refreshToken;
        return Task.CompletedTask;
    }

    public Task RevokeFamilyAsync(Guid familyId, DateTime revokedAtUtc, string reason)
    {
        RevokeFamilyCalls.Add((familyId, revokedAtUtc, reason));
        return Task.CompletedTask;
    }

    public Task RevokeActiveByUserIdAsync(Guid userId, DateTime revokedAtUtc, string reason)
    {
        RevokeUserCalls.Add((userId, revokedAtUtc, reason));
        return Task.CompletedTask;
    }

    public void Store(RefreshToken refreshToken)
    {
        _refreshTokensByHash[refreshToken.TokenHash] = refreshToken;
    }
}

internal sealed class FakePasswordEncripter : IPasswordEncripter
{
    public bool IsValidResult { get; set; } = true;

    public string Encrypt(string password)
    {
        return $"hashed::{password}";
    }

    public bool IsValid(string password, string passwordHash)
    {
        return IsValidResult;
    }
}

internal sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
{
    public AccessTokenResult Result { get; set; } = new()
    {
        Token = "access-token",
        TokenId = Guid.NewGuid(),
        ExpiresAtUtc = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc)
    };

    public User? LastGeneratedUser { get; private set; }

    public AccessTokenResult Generate(User user)
    {
        LastGeneratedUser = user;
        return Result;
    }
}

internal sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public RefreshTokenMaterial Material { get; set; } = new()
    {
        Token = "refresh-token",
        Hash = "refresh-token-hash"
    };

    public DateTime ExpiresAtUtc { get; set; } = new DateTime(2026, 4, 27, 12, 0, 0, DateTimeKind.Utc);

    public RefreshTokenMaterial Create()
    {
        return Material;
    }

    public string ComputeHash(string refreshToken)
    {
        return $"{refreshToken.Trim().ToLowerInvariant()}-hash";
    }

    public DateTime GetExpiresAtUtc()
    {
        return ExpiresAtUtc;
    }
}

internal sealed class SpyUnitOfWork : IUnitOfWork
{
    public int BegunTransactions { get; private set; }

    public int CommittedTransactions { get; private set; }

    public int RolledBackTransactions { get; private set; }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        BegunTransactions++;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommittedTransactions++;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RolledBackTransactions++;
        return Task.CompletedTask;
    }
}

internal static class AuthenticationFixtures
{
    public static User CreateVerifiedUser(Guid? id = null, bool isActive = true)
    {
        var now = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc);

        return User.Restore(
            id ?? Guid.NewGuid(),
            now.AddDays(-30),
            now.AddDays(-1),
            isActive,
            "Bruno",
            "Silva",
            "Bruno Silva",
            $"bruno.{Guid.NewGuid():N}@authcore.dev",
            "11999999999",
            Role.User,
            Guid.NewGuid(),
            now.AddDays(-10));
    }

    public static User CreateUnverifiedUser(Guid? id = null)
    {
        var now = new DateTime(2026, 4, 13, 12, 0, 0, DateTimeKind.Utc);

        return User.Restore(
            id ?? Guid.NewGuid(),
            now.AddDays(-30),
            now.AddDays(-1),
            true,
            "Bruno",
            "Silva",
            "Bruno Silva",
            $"bruno.{Guid.NewGuid():N}@authcore.dev",
            "11999999999",
            Role.User,
            Guid.NewGuid(),
            null);
    }

    public static Password CreatePassword(
        Guid userId,
        PasswordStatus status = PasswordStatus.Active,
        int failedAttempts = 0)
    {
        var password = Password.Create(userId, "stored-password-hash", status);

        for (var index = 0; index < failedAttempts; index++)
            password = password.RegisterLoginFailure();

        return password;
    }
}
