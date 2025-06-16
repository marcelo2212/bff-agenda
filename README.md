# BFF Agenda

Este projeto é um Backend for Frontend (BFF) desenvolvido em .NET 8. Ele atua como uma ponte entre o front-end e a `api-agenda`, utilizando mensageria assíncrona com RabbitMQ no padrão RPC (Remote Procedure Call).

---

## Tecnologias Utilizadas

- .NET 8
- MediatR (CQRS)
- RabbitMQ
- JWT (autenticação)
- Swagger
- FluentValidation
- Docker e Docker Compose
- xUnit (testes)

---

## Funcionalidades

- Autenticação via JWT
- Consumo de eventos RabbitMQ de forma assíncrona
- CRUD completo de contatos
- Integração segura entre front-end e a API de contatos
- Documentação interativa com Swagger
- Testes automatizados com xUnit

---

## Estrutura do Projeto

```text
bff-agenda/
├── BffAgenda.API/             # Camada de entrada - Controllers e Middlewares
├── BffAgenda.Application/     # DTOs, Commands, Queries, Validators
├── BffAgenda.Domain/          # Entidades e contratos
├── BffAgenda.Infrastructure/  # Mensageria (RabbitMQ), Handlers
├── BffAgenda.Tests/           # Testes automatizados (xUnit)
├── docker-compose.yml         # Orquestração com Docker
└── .env                       # Variáveis de ambiente
```
---

## Executando com Docker

```bash
docker-compose up
```
---

## Acessos Locais

- API Swagger: http://localhost:5000/swagger
  
