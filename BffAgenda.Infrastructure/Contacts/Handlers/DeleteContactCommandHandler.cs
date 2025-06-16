using System;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.Commands;
using BffAgenda.Infrastructure.Contacts.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BffAgenda.Infrastructure.Contacts.Handlers.Commands;

public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, string>
{
    private readonly ContactProducer _producer;
    private readonly ILogger<DeleteContactCommandHandler> _logger;

    public DeleteContactCommandHandler(
        ContactProducer producer,
        ILogger<DeleteContactCommandHandler> logger
    )
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<string> Handle(
        DeleteContactCommand request,
        CancellationToken cancellationToken
    )
    {
        return await _producer.PublishDeleteContactAsync(request.Id, cancellationToken);
    }
}
