namespace AuthCore.Application.Common.Models.Responses;

/// <summary>
/// Representa resposta de erro da aplicação.
/// </summary>
public sealed class ResponseErrorJson
{
    /// <summary>
    /// Lista de mensagens de erro retornadas pela aplicação.
    /// </summary>
    public IList<string> Errors { get; set; } = [];
}
