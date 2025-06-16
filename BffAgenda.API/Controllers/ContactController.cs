using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BffAgenda.Application.Contacts.Commands;
using BffAgenda.Application.Contacts.DTOs;
using BffAgenda.Application.Contacts.Queries;
using BffAgenda.Infrastructure.Contacts.Handlers;
using BffAgenda.Infrastructure.Contacts.Messaging;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BffAgenda.API.Controllers
{
    [ApiController]
    [Route("contacts")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly ILogger<ContactsController> _logger;
        private readonly ContactProducer _producer;
        private readonly IMediator _mediator;

        public ContactsController(
            ILogger<ContactsController> logger,
            ContactProducer producer,
            IMediator mediator
        )
        {
            _logger = logger;
            _producer = producer;
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateContactDto dto,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var response = await _producer.PublishCreateContactAsync(dto, cancellationToken);
                return Ok(response);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Timeout ao tentar criar contato via RabbitMQ.");
                return StatusCode(504, new { message = "Timeout ao aguardar resposta da fila." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao criar contato.");
                return StatusCode(500, new { message = "Erro interno ao criar contato." });
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<ContactResponseDto>>> GetAllContacts(
            CancellationToken cancellationToken
        )
        {
            try
            {
                var result = await _mediator.Send(
                    new BffAgenda.Application.Contacts.Queries.GetAllContactsQuery(),
                    cancellationToken
                );

                return Ok(result);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Timeout ao tentar obter contatos via RabbitMQ.");
                return StatusCode(504, new { message = "Timeout ao aguardar resposta da fila." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao listar contatos.");
                return StatusCode(500, new { message = "Erro interno ao listar contatos." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactResponseDto>> GetContactById(
            string id,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new { message = "ID inválido." });
            }

            try
            {
                var result = await _mediator.Send(
                    new GetContactByIdQuery(guidId),
                    cancellationToken
                );

                if (result == null)
                {
                    return NotFound(new { message = "Contato não encontrado." });
                }

                _logger.LogInformation("Contato encontrado: {@Contato}", result);
                return Ok(result);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Timeout ao tentar obter contato via RabbitMQ.");
                return StatusCode(504, new { message = "Timeout ao aguardar resposta da fila." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao obter contato.");
                return StatusCode(500, new { message = "Erro interno ao obter contato." });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] UpdateContactDto dto,
            CancellationToken cancellationToken
        )
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return BadRequest(new { message = "ID inválido." });
            }

            dto.Id = guidId;

            try
            {
                var result = await _mediator.Send(new UpdateContactCommand(dto), cancellationToken);
                return Ok(result);
            }
            catch (TimeoutException tex)
            {
                _logger.LogWarning(tex, "Timeout ao tentar atualizar contato via RabbitMQ.");
                return StatusCode(504, new { message = "Timeout ao aguardar resposta da fila." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao atualizar contato.");
                return StatusCode(500, new { message = "Erro interno ao atualizar contato." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new DeleteContactCommand(id), cancellationToken);
                return Ok(new { message = result });
            }
            catch (TimeoutException ex)
            {
                _logger.LogWarning(ex, "Timeout ao deletar contato.");
                return StatusCode(504, "Timeout ao deletar contato via RabbitMQ.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao deletar contato.");
                return StatusCode(500, "Erro interno ao deletar contato.");
            }
        }
    }
}
