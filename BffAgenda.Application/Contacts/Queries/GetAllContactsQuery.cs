using System.Collections.Generic;
using BffAgenda.Application.Contacts.DTOs;
using MediatR;

namespace BffAgenda.Application.Contacts.Queries
{
    public class GetAllContactsQuery : IRequest<List<ContactResponseDto>> { }
}
