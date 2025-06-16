namespace BffAgenda.Application.Contacts.DTOs;

public class CreateContactDto
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
}
