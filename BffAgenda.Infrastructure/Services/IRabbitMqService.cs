using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BffAgenda.Infrastructure.Services
{
    public interface IRabbitMqService
    {
        Task<string> PublishRpcAsync(string queueName, object message, TimeSpan timeout);
        IModel CreateChannel();
    }
}
