namespace AuthCore.Domain.Common.Exceptions;

/// <summary>
/// Representa exceção para recurso não encontrado.
/// </summary>
public sealed class NotFoundException : DomainException
{
    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="message">Mensagem que descreve o erro.</param>
    public NotFoundException(string message) : base(message) { }

    #endregion
}
