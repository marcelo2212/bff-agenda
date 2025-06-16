using BffAgenda.Application.Contacts.DTOs;
using MediatR;

namespace BffAgenda.Application.Contacts.Commands;

public class CreateContactCommand : IRequest<ContactResponseDto>
{
    public CreateContactDto Contact { get; }

    public CreateContactCommand(CreateContactDto contact)
    {
        Contact = contact;
    }
}
