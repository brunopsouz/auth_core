namespace AuthCore.Api.Security;

/// <summary>
/// Representa o resultado da avaliação de limite de tentativas de login.
/// </summary>
public sealed class LoginRateLimitResult
{
    /// <summary>
    /// Indica se a tentativa atual pode prosseguir.
    /// </summary>
    public bool IsAllowed { get; private init; }

    /// <summary>
    /// Tempo sugerido para nova tentativa quando a operação está bloqueada.
    /// </summary>
    public TimeSpan RetryAfter { get; private init; }

    /// <summary>
    /// Operação para criar um resultado permitido.
    /// </summary>
    /// <returns>Resultado com permissão para prosseguir.</returns>
    public static LoginRateLimitResult Allow()
    {
        return new LoginRateLimitResult
        {
            IsAllowed = true,
            RetryAfter = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Operação para criar um resultado bloqueado.
    /// </summary>
    /// <param name="retryAfter">Tempo sugerido para uma nova tentativa.</param>
    /// <returns>Resultado bloqueado.</returns>
    public static LoginRateLimitResult Block(TimeSpan retryAfter)
    {
        return new LoginRateLimitResult
        {
            IsAllowed = false,
            RetryAfter = retryAfter > TimeSpan.Zero
                ? retryAfter
                : TimeSpan.Zero
        };
    }
}
