namespace AuthCore.Api.Contracts.Responses;

/// <summary>
/// Representa resposta da listagem de sessões ativas do usuário.
/// </summary>
public sealed class ResponseUserSessionsJson
{
    /// <summary>
    /// Identificador da sessão atual.
    /// </summary>
    public string CurrentSid { get; set; } = string.Empty;

    /// <summary>
    /// Sessões ativas do usuário.
    /// </summary>
    public IReadOnlyCollection<ResponseUserSessionJson> Sessions { get; set; } = [];
}
