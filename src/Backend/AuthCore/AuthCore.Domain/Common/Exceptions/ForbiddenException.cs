namespace AuthCore.Domain.Common.Exceptions;

/// <summary>
/// Representa uma exceção de acesso proibido para o contexto atual.
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="message">Mensagem descritiva da falha de autorização.</param>
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
