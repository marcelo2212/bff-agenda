using System;

namespace BffAgenda.Application.Contacts.DTOs;

public class ContactResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
}
