namespace AuthCore.Api.Security;

/// <summary>
/// Define operação para validar origem de requisições autenticadas por cookie.
/// </summary>
public interface ICsrfRequestValidator
{
    /// <summary>
    /// Operação para validar a origem da requisição HTTP atual.
    /// </summary>
    /// <param name="request">Requisição autenticada por cookie.</param>
    void Validate(HttpRequest request);
}
