using System.Diagnostics;
using System.Text.Json;
using AuthCore.Domain.Common.DomainEvents;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Security.Emails;
using AuthCore.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthCore.Infrastructure.Services.Messaging;

/// <summary>
/// Representa processor das mensagens pendentes da outbox.
/// </summary>
public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly OutboxOptions _outboxOptions;
    private readonly OutboxMetrics _outboxMetrics;
    private readonly ILogger<OutboxProcessor> _logger;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="outboxRepository">Repositório da outbox.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    /// <param name="emailSender">Sender de e-mail.</param>
    /// <param name="outboxOptions">Opções de processamento da outbox.</param>
    /// <param name="outboxMetrics">Métricas da outbox.</param>
    /// <param name="logger">Serviço de logging.</param>
    public OutboxProcessor(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IOptions<OutboxOptions> outboxOptions,
        OutboxMetrics outboxMetrics,
        ILogger<OutboxProcessor> logger)
    {
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _outboxOptions = outboxOptions.Value;
        _outboxMetrics = outboxMetrics;
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Operação para processar mensagens pendentes da outbox.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Resultado do ciclo de processamento.</returns>
    public async Task<OutboxProcessingResult> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var messages = await _outboxRepository.GetPendingAsync(
                _outboxOptions.BatchSize,
                _outboxOptions.MaxAttempts);
            var processedCount = 0;
            var failedCount = 0;

            foreach (var message in messages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await TryProcessMessageAsync(message, cancellationToken))
                    processedCount++;
                else
                    failedCount++;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            var result = new OutboxProcessingResult
            {
                ProcessedCount = processedCount,
                FailedCount = failedCount
            };

            _logger.LogInformation(
                "Ciclo da outbox concluído. Total={TotalCount}, Processadas={ProcessedCount}, Falhas={FailedCount}.",
                result.TotalCount,
                result.ProcessedCount,
                result.FailedCount);

            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _outboxMetrics.RecordDuration(stopwatch.Elapsed);
        }
    }

    #region Helpers

    /// <summary>
    /// Operação para processar uma mensagem da outbox.
    /// </summary>
    /// <param name="message">Mensagem pendente.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    /// <returns>Indicador de sucesso no processamento.</returns>
    private async Task<bool> TryProcessMessageAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            await DispatchAsync(message, cancellationToken);

            var processedMessage = message.MarkAsProcessed(DateTime.UtcNow);
            await _outboxRepository.UpdateAsync(processedMessage);
            _outboxMetrics.RecordProcessed(message.Type);

            _logger.LogInformation(
                "Mensagem de outbox processada. MessageId={MessageId}, Type={MessageType}.",
                message.Id,
                message.Type);

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedMessage = message.RegisterFailure(GetErrorMessage(exception));
            await _outboxRepository.UpdateAsync(failedMessage);
            _outboxMetrics.RecordFailed(message.Type);

            _logger.LogWarning(
                exception,
                "Falha ao processar mensagem de outbox. MessageId={MessageId}, Type={MessageType}, AttemptCount={AttemptCount}.",
                message.Id,
                message.Type,
                failedMessage.AttemptCount);

            return false;
        }
    }

    /// <summary>
    /// Operação para despachar a mensagem para o handler correspondente.
    /// </summary>
    /// <param name="message">Mensagem pendente.</param>
    /// <param name="cancellationToken">Token para cancelamento da operação.</param>
    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (message.Type != nameof(EmailVerificationRequested))
            throw new InvalidOperationException($"Tipo de mensagem de outbox não suportado: {message.Type}.");

        var outboxEvent = JsonSerializer.Deserialize<EmailVerificationRequested>(message.Content)
            ?? throw new InvalidOperationException("Conteúdo da mensagem de outbox inválido.");

        outboxEvent.Validate();

        await _emailSender.SendEmailVerificationAsync(
            outboxEvent.Email,
            outboxEvent.Code,
            cancellationToken);
    }

    /// <summary>
    /// Operação para obter a mensagem de erro persistida.
    /// </summary>
    /// <param name="exception">Exceção capturada.</param>
    /// <returns>Mensagem de erro normalizada.</returns>
    private static string GetErrorMessage(Exception exception)
    {
        return string.IsNullOrWhiteSpace(exception.Message)
            ? exception.GetType().Name
            : exception.Message;
    }

    #endregion
}
