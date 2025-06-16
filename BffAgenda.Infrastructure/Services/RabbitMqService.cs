using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BffAgenda.Infrastructure.Services
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(ILogger<RabbitMqService> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "guest",
                DispatchConsumersAsync = false,
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        public async Task<string> PublishRpcAsync(
            string queueName,
            object message,
            TimeSpan timeout
        )
        {
            var correlationId = Guid.NewGuid().ToString();
            var replyQueueName = _channel.QueueDeclare().QueueName;

            var props = _channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: props,
                body: body
            );

            var tcs = new TaskCompletionSource<string>();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                    tcs.TrySetResult(response);
                }
                await Task.Yield();
            };

            _channel.BasicConsume(consumer: consumer, queue: replyQueueName, autoAck: true);

            if (await Task.WhenAny(tcs.Task, Task.Delay(timeout)) == tcs.Task)
            {
                return await tcs.Task;
            }

            throw new TimeoutException("Timeout ao aguardar resposta RPC do RabbitMQ.");
        }

        public IModel CreateChannel()
        {
            return _connection.CreateModel();
        }
    }
}
