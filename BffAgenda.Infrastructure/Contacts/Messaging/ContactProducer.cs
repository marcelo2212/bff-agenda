using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.DTOs;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BffAgenda.Infrastructure.Contacts.Messaging
{
    public class ContactProducer
    {
        private readonly IModel _channel;
        private readonly ILogger<ContactProducer> _logger;
        private readonly ConcurrentDictionary<
            string,
            TaskCompletionSource<object>
        > _pendingResponses;

        private readonly string _replyQueueCreate = "contacts.rpc.reply";
        private readonly string _replyQueueUpdate = "contacts.rpc.reply.update";
        private readonly string _replyQueueDelete = "contacts.rpc.reply.delete";

        public ContactProducer(IModel channel, ILogger<ContactProducer> logger)
        {
            _channel = channel;
            _logger = logger;
            _pendingResponses = new();

            foreach (var queue in new[] { _replyQueueCreate, _replyQueueUpdate, _replyQueueDelete })
            {
                _channel.QueueDeclare(queue, durable: false, exclusive: false, autoDelete: false);
            }
        }

        public void StartConsuming()
        {
            ConsumeReplyQueue(_replyQueueCreate, isString: false);
            ConsumeReplyQueue(_replyQueueUpdate, isString: false);
            ConsumeReplyQueue(_replyQueueDelete, isString: true);
        }

        private void ConsumeReplyQueue(string queueName, bool isString)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var correlationId = ea.BasicProperties?.CorrelationId;
                var rawBody = Encoding.UTF8.GetString(ea.Body.Span);

                if (string.IsNullOrWhiteSpace(correlationId))
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                if (_pendingResponses.TryRemove(correlationId, out var tcs))
                {
                    try
                    {
                        if (isString)
                        {
                            tcs.SetResult(rawBody);
                        }
                        else
                        {
                            var response = JsonSerializer.Deserialize<ContactResponseDto>(
                                rawBody,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );
                            if (response == null)
                                throw new Exception("Resposta nula recebida.");

                            tcs.SetResult(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Erro ao processar resposta da fila {Queue}",
                            queueName
                        );
                        tcs.SetException(ex);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "CorrelationId {CorrelationId} não encontrado.",
                        correlationId
                    );
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        public async Task<ContactResponseDto> PublishCreateContactAsync(
            CreateContactDto dto,
            CancellationToken cancellationToken = default
        )
        {
            return (ContactResponseDto)
                await PublishAsync("contacts.create", dto, _replyQueueCreate, cancellationToken);
        }

        public async Task<ContactResponseDto> PublishUpdateContactAsync(
            UpdateContactDto dto,
            CancellationToken cancellationToken = default
        )
        {
            var payload = new { Id = dto.Id, Contact = dto };
            return (ContactResponseDto)
                await PublishAsync(
                    "contacts.update",
                    payload,
                    _replyQueueUpdate,
                    cancellationToken
                );
        }

        public async Task<string> PublishDeleteContactAsync(
            Guid id,
            CancellationToken cancellationToken = default
        )
        {
            return (string)
                await PublishAsync(
                    "contacts.delete",
                    id.ToString(),
                    _replyQueueDelete,
                    cancellationToken
                );
        }

        private async Task<object> PublishAsync(
            string routingKey,
            object payload,
            string replyQueue,
            CancellationToken cancellationToken
        )
        {
            var correlationId = Guid.NewGuid().ToString();
            var isStringResponse = replyQueue == _replyQueueDelete;

            var tcs = isStringResponse
                ? new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously
                )
                : new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously
                );

            _pendingResponses[correlationId] = tcs;

            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                var props = _channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueue;

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: routingKey,
                    basicProperties: props,
                    body: body
                );

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken
                );
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

                using (
                    timeoutCts.Token.Register(() =>
                    {
                        tcs.TrySetException(
                            new TimeoutException("Tempo excedido aguardando resposta da fila.")
                        );
                    })
                )
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao publicar ou aguardar resposta ({RoutingKey})",
                    routingKey
                );
                throw;
            }
            finally
            {
                _pendingResponses.TryRemove(correlationId, out _);
            }
        }
    }
}
