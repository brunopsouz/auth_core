using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Passports.Repositories;
using AuthCore.Domain.Security.Tokens.Services;

namespace AuthCore.Application.Authentication.UseCases.LogoutSession;

/// <summary>
/// Representa caso de uso para encerrar uma sessão autenticada.
/// </summary>
public sealed class LogoutSessionUseCase : ILogoutSessionUseCase
{
    private const string LOGOUT_REASON = "logout";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUnitOfWork _unitOfWork;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="refreshTokenRepository">Repositório de refresh token.</param>
    /// <param name="refreshTokenService">Serviço de refresh token.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public LogoutSessionUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _unitOfWork = unitOfWork;
    }

    #endregion

    /// <summary>
    /// Operação para encerrar uma sessão autenticada.
    /// </summary>
    /// <param name="command">Comando com o refresh token informado.</param>
    public async Task Execute(LogoutSessionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tokenHash = _refreshTokenService.ComputeHash(command.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);

        if (refreshToken is null || refreshToken.RevokedAtUtc.HasValue)
            return;

        if (refreshToken.ConsumedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(refreshToken.FamilyId);
            return;
        }

        var nowUtc = DateTime.UtcNow;

        if (!refreshToken.IsActiveAt(nowUtc))
            return;

        var revokedRefreshToken = refreshToken.Revoke(LOGOUT_REASON, nowUtc);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _refreshTokenRepository.UpdateAsync(revokedRefreshToken);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    #region Helpers

    /// <summary>
    /// Operação para revogar a família da sessão encerrada.
    /// </summary>
    /// <param name="familyId">Identificador da família de rotação.</param>
    private async Task RevokeFamilyAsync(Guid familyId)
    {
        var revokedAtUtc = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _refreshTokenRepository.RevokeFamilyAsync(familyId, revokedAtUtc, LOGOUT_REASON);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    #endregion
}
