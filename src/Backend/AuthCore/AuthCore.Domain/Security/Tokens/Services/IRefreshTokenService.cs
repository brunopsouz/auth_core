using AuthCore.Domain.Security.Tokens.Models;

namespace AuthCore.Domain.Security.Tokens.Services;

/// <summary>
/// Define operações para gerar e processar refresh token.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Operação para criar um refresh token.
    /// </summary>
    /// <returns>Material do refresh token gerado.</returns>
    RefreshTokenMaterial Create();

    /// <summary>
    /// Operação para calcular o hash persistido de um refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token em texto puro.</param>
    /// <returns>Hash persistido do refresh token.</returns>
    string ComputeHash(string refreshToken);

    /// <summary>
    /// Operação para obter a data de expiração de um refresh token em UTC.
    /// </summary>
    /// <returns>Data de expiração calculada para o refresh token.</returns>
    DateTime GetExpiresAtUtc();
}
