using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.DTOs;
using BffAgenda.Application.Contacts.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BffAgenda.Infrastructure.Contacts.Handlers;

public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, ContactResponseDto>
{
    private readonly IModel _channel;
    private readonly ILogger<GetContactByIdQueryHandler> _logger;
    private readonly string _replyQueueName = "contacts.rpc.reply.getbyid";
    private static readonly ConcurrentDictionary<
        string,
        TaskCompletionSource<ContactResponseDto>
    > _pendingResponses = new();

    private bool _consumerInitialized;

    public GetContactByIdQueryHandler(IModel channel, ILogger<GetContactByIdQueryHandler> logger)
    {
        _channel = channel;
        _logger = logger;

        _channel.QueueDeclare(
            queue: _replyQueueName,
            durable: false,
            exclusive: false,
            autoDelete: false
        );
        StartConsuming();
    }

    private void StartConsuming()
    {
        if (_consumerInitialized)
            return;
        _consumerInitialized = true;
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (_, ea) =>
        {
            var correlationId = ea.BasicProperties?.CorrelationId;
            var rawBody = Encoding.UTF8.GetString(ea.Body.Span);

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.LogWarning("Mensagem sem CorrelationId. Ignorada.");
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            if (_pendingResponses.TryRemove(correlationId, out var tcs))
            {
                try
                {
                    var response = JsonSerializer.Deserialize<ContactResponseDto>(
                        rawBody,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                        }
                    );

                    if (response == null)
                    {
                        _logger.LogError(
                            "Resposta desserializada nula. CorrelationId: {CorrelationId}",
                            correlationId
                        );
                        tcs.SetException(new Exception("Resposta nula recebida."));
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Resposta processada com sucesso. CorrelationId: {CorrelationId}",
                            correlationId
                        );
                        _logger.LogInformation(
                            "Resposta da API Agenda: {Json}",
                            JsonSerializer.Serialize(
                                response,
                                new JsonSerializerOptions { WriteIndented = true }
                            )
                        );
                        tcs.SetResult(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Erro ao desserializar resposta. CorrelationId: {CorrelationId}",
                        correlationId
                    );
                    tcs.SetException(ex);
                }
            }
            else
            {
                _logger.LogWarning(
                    "CorrelationId {CorrelationId} não encontrado no _pendingResponses",
                    correlationId
                );
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue: _replyQueueName, autoAck: false, consumer: consumer);
    }

    public async Task<ContactResponseDto> Handle(
        GetContactByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<ContactResponseDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _pendingResponses[correlationId] = tcs;

        var props = _channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = _replyQueueName;

        var payload = JsonSerializer.Serialize(new { id = request.Id.ToString() });
        var body = Encoding.UTF8.GetBytes(payload);

        try
        {
            _channel.BasicPublish(
                exchange: "",
                routingKey: "contacts.getbyid",
                basicProperties: props,
                body: body
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem para contacts.getbyid");
            _pendingResponses.TryRemove(correlationId, out _);
            throw;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

        using (
            timeoutCts.Token.Register(() =>
            {
                if (_pendingResponses.TryRemove(correlationId, out var source))
                {
                    _logger.LogWarning(
                        "Timeout para CorrelationId: {CorrelationId}",
                        correlationId
                    );
                    source.TrySetException(new TimeoutException("Tempo excedido para resposta."));
                }
            })
        )
        {
            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
