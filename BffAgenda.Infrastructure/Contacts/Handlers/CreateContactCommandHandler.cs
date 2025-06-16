using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.Commands;
using BffAgenda.Application.Contacts.DTOs;
using BffAgenda.Infrastructure.Contacts.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffAgenda.Application.Contacts.Handlers
{
    public class CreateContactCommandHandler
        : IRequestHandler<CreateContactCommand, ContactResponseDto>
    {
        private readonly ContactProducer _contactProducer;
        private readonly ILogger<CreateContactCommandHandler> _logger;

        public CreateContactCommandHandler(
            ContactProducer contactProducer,
            ILogger<CreateContactCommandHandler> logger
        )
        {
            _contactProducer = contactProducer;
            _logger = logger;
        }

        public async Task<ContactResponseDto> Handle(
            CreateContactCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var payload = JsonSerializer.Serialize(request.Contact);
                var response = await _contactProducer.PublishCreateContactAsync(request.Contact);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar contato via RabbitMQ.");
                throw;
            }
        }
    }
}
