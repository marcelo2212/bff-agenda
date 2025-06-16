using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

public class GetAllContactsQueryHandler
    : IRequestHandler<GetAllContactsQuery, List<ContactResponseDto>>
{
    private readonly IModel _channel;
    private readonly ILogger<GetAllContactsQueryHandler> _logger;
    private readonly string _replyQueueName = "contacts.rpc.reply.getall";
    private static readonly ConcurrentDictionary<
        string,
        TaskCompletionSource<List<ContactResponseDto>>
    > _pendingResponses = new();
    private bool _consumerInitialized = false;

    public GetAllContactsQueryHandler(IModel channel, ILogger<GetAllContactsQueryHandler> logger)
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
                _logger.LogWarning("Mensagem recebida sem CorrelationId. Ignorada.");
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            if (_pendingResponses.TryRemove(correlationId, out var tcs))
            {
                try
                {
                    var response = JsonSerializer.Deserialize<List<ContactResponseDto>>(
                        rawBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (response == null)
                    {
                        _logger.LogError("Resposta nula. Payload: {Payload}", rawBody);
                        tcs.SetException(new Exception("Resposta nula recebida do consumidor."));
                    }
                    else
                    {
                        tcs.SetResult(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao desserializar resposta.");
                    tcs.SetException(ex);
                }
            }
            else
            {
                _logger.LogWarning("CorrelationId {CorrelationId} não encontrado.", correlationId);
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(queue: _replyQueueName, autoAck: false, consumer: consumer);
    }

    public async Task<List<ContactResponseDto>> Handle(
        GetAllContactsQuery request,
        CancellationToken cancellationToken
    )
    {
        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<List<ContactResponseDto>>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _pendingResponses[correlationId] = tcs;

        var props = _channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = _replyQueueName;

        try
        {
            _channel.BasicPublish(
                exchange: "",
                routingKey: "contacts.getall",
                basicProperties: props,
                body: Encoding.UTF8.GetBytes("")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem na fila 'contacts.getall'");
            _pendingResponses.TryRemove(correlationId, out _);
            throw;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

        using (
            timeoutCts.Token.Register(() =>
            {
                if (_pendingResponses.TryRemove(correlationId, out var source))
                {
                    _logger.LogWarning(
                        "Timeout: resposta não recebida para CorrelationId {CorrelationId}",
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
