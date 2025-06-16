using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.Commands;
using BffAgenda.Application.Contacts.DTOs;
using BffAgenda.Infrastructure.Contacts.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffAgenda.Infrastructure.Contacts.Handlers.Commands
{
    public class UpdateContactCommandHandler
        : IRequestHandler<UpdateContactCommand, ContactResponseDto>
    {
        private readonly ContactProducer _contactProducer;
        private readonly ILogger<UpdateContactCommandHandler> _logger;

        public UpdateContactCommandHandler(
            ContactProducer contactProducer,
            ILogger<UpdateContactCommandHandler> logger
        )
        {
            _contactProducer = contactProducer;
            _logger = logger;
        }

        public async Task<ContactResponseDto> Handle(
            UpdateContactCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var payload = JsonSerializer.Serialize(request.Contact);
                var response = await _contactProducer.PublishUpdateContactAsync(
                    request.Contact,
                    cancellationToken
                );
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar contato via RabbitMQ.");
                throw;
            }
        }
    }
}
