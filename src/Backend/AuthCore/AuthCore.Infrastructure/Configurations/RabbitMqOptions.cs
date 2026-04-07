using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configurations;

/// <summary>
/// Representa as configurações de conexão com o RabbitMQ.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// Endereço do servidor RabbitMQ.
    /// </summary>
    [Required]
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// Porta de conexão do RabbitMQ.
    /// </summary>
    public int Port { get; init; } = 5672;

    /// <summary>
    /// Nome do usuário de acesso.
    /// </summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Senha do usuário de acesso.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Nome da fila de verificação de e-mail.
    /// </summary>
    [Required]
    public string EmailVerificationQueue { get; init; } = "auth.email-verification";
}
