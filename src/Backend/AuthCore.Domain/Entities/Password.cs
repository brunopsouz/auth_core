using AuthCore.Domain.Common.Enums;

namespace AuthCore.Domain.Entities;

/// <summary>
/// Entidade responsável pelo gerenciamento da senha do usuário.
/// </summary>
public sealed class Password
{
    /// <summary>
    /// Identificador único do usuário associado à senha.
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// Valor da senha armazenada para o usuário.
    /// </summary>
    public string Value { get; private set; } = null!;

    /// <summary>
    /// Chave secreta utilizada para proteger ou validar a senha.
    /// </summary>
    public string SecretKey { get; private set; } = null!;

    /// <summary>
    /// Número de tentativas de acesso realizadas pelo usuário.
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// Status atual da senha (ex.: ativa, bloqueada, expirada).
    /// </summary>
    public PasswordStatus Status { get; private set; }

}