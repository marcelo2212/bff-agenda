using MediatR;

namespace BffAgenda.Application.Contacts.Commands;

public class DeleteContactCommand : IRequest<string>
{
    public Guid Id { get; }

    public DeleteContactCommand(Guid id)
    {
        Id = id;
    }
}
