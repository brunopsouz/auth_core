namespace AuthCore.Domain.Entities;

/// <summary>
/// Controla tentativas de login e bloqueios temporários.
/// </summary>
public sealed class LoginAttempt
{

    #region Constants
    private const int MAX_ATTEMPTS = 5;
    private static readonly TimeSpan LOCK_DURATION = TimeSpan.FromMinutes(15);
    #endregion

    public int FailedAttempts { get; private set; }

    public DateTime? LastFailedAt { get; private set; }

    public DateTime? LockedUntil { get; private set; }

    private LoginAttempt(
        int failedAttempts,
        DateTime? lastFailedAt,
        DateTime? lockedUntil)
    {
        FailedAttempts = failedAttempts;
        LastFailedAt = lastFailedAt;
        LockedUntil = lockedUntil;
    }

    private LoginAttempt() { }

    #region Factory

    /// <summary>
    /// Cria um novo controle de tentativas.
    /// </summary>
    /// <returns>Instância inicial de <see cref="LoginAttempt"/>.</returns>
    public static LoginAttempt Create()
    {
        return new LoginAttempt(0, null, null);
    }

    /// <summary>
    /// Restaura um controle de tentativas já persistido.
    /// </summary>
    /// <param name="failedAttempts">Quantidade de tentativas falhas.</param>
    /// <param name="lastFailedAt">Data/hora da última falha.</param>
    /// <param name="lockedUntil">Data/hora de expiração do bloqueio.</param>
    /// <returns>Instância restaurada de <see cref="LoginAttempt"/>.</returns>
    public static LoginAttempt Restore(
        int failedAttempts,
        DateTime? lastFailedAt,
        DateTime? lockedUntil)
    {
        return new LoginAttempt(failedAttempts, lastFailedAt, lockedUntil);
    }

    #endregion

    /// <summary>
    /// Registra falha e aplica bloqueio quando necessário.
    /// </summary>
    /// <returns>Instância atualizada com os novos valores.</returns>
    public LoginAttempt RegisterFailure()
    {
        var now = DateTime.UtcNow;
        var failed = FailedAttempts + 1;
        var lockedUntil = failed >= MAX_ATTEMPTS
            ? now.Add(LOCK_DURATION)
            : LockedUntil;
        return new LoginAttempt(failed, now, lockedUntil);
    }

    /// <summary>
    /// Reseta contadores e remove bloqueios.
    /// </summary>
    /// <returns>Instância limpa de tentativas.</returns>
    public LoginAttempt Reset()
    {
        return new LoginAttempt(0, null, null);
    }

    /// <summary>
    /// Verifica se a conta está bloqueada.
    /// </summary>
    /// <returns>True quando o bloqueio está ativo.</returns>
    public bool IsLocked()
    {
        return LockedUntil.HasValue && LockedUntil > DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém mensagem com o tempo restante de bloqueio.
    /// </summary>
    /// <returns>Mensagem ou null quando não há bloqueio.</returns>
    public string? GetLockMessage()
    {
        if (!IsLocked())
            return null;
        var remaining = LockedUntil!.Value - DateTime.UtcNow;
        if (remaining.TotalSeconds < 60)
            return $"A conta está temporariamente bloqueada. Tente novamente em {Math.Ceiling(remaining.TotalSeconds)} segundos.";
        return $"A conta está temporariamente bloqueada. Tente novamente em {Math.Ceiling(remaining.TotalMinutes)} minutos.";
    }
}