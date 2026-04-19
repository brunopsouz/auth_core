using System.ComponentModel.DataAnnotations;

namespace AuthCore.Infrastructure.Configurations;

/// <summary>
/// Representa as configurações do processamento da outbox.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Nome da seção de configuração.
    /// </summary>
    public const string SectionName = "Outbox";

    /// <summary>
    /// Quantidade de mensagens processadas por ciclo.
    /// </summary>
    [Range(1, 500)]
    public int BatchSize { get; init; } = 20;

    /// <summary>
    /// Intervalo de polling em segundos.
    /// </summary>
    [Range(1, 300)]
    public int PollingIntervalSeconds { get; init; } = 10;
}
