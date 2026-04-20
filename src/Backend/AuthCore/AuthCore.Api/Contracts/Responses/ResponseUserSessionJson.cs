namespace AuthCore.Api.Contracts.Responses;

/// <summary>
/// Representa resposta de uma sessão ativa do usuário.
/// </summary>
public sealed class ResponseUserSessionJson
{
    /// <summary>
    /// Identificador público da sessão.
    /// </summary>
    public string Sid { get; set; } = string.Empty;

    /// <summary>
    /// Data de criação da sessão em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Data do último uso da sessão em UTC.
    /// </summary>
    public DateTime? LastSeenAtUtc { get; set; }

    /// <summary>
    /// Endereço IP associado à sessão.
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// User-Agent associado à sessão.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Data de expiração da sessão em UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }
}
