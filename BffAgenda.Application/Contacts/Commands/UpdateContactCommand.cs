using BffAgenda.Application.Contacts.DTOs;
using MediatR;

namespace BffAgenda.Application.Contacts.Commands;

public class UpdateContactCommand : IRequest<ContactResponseDto>
{
    public UpdateContactDto Contact { get; }

    public UpdateContactCommand(UpdateContactDto contact)
    {
        Contact = contact;
    }
}
