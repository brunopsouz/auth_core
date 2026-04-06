using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configuration;

/// <summary>
/// Configuração de mensageria usada para eventos de autenticação.
/// </summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 5672;

    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string EmailVerificationQueue { get; init; } = "auth.email-verification";
}
