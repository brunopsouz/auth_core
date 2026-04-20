namespace AuthCore.Api.Security;

/// <summary>
/// Define operação para limitar tentativas de login.
/// </summary>
public interface ILoginRateLimiter
{
    /// <summary>
    /// Operação para registrar uma tentativa de login.
    /// </summary>
    /// <param name="ipAddress">Endereço IP da tentativa.</param>
    /// <param name="email">E-mail informado no login.</param>
    /// <returns>Resultado da avaliação do limite atual.</returns>
    Task<LoginRateLimitResult> TryAcquireAsync(string? ipAddress, string? email);
}
