using BffAgenda.Application.Contacts.DTOs;
using MediatR;

namespace BffAgenda.Application.Contacts.Queries
{
    public class GetContactByIdQuery : IRequest<ContactResponseDto>
    {
        public Guid Id { get; }

        public GetContactByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
