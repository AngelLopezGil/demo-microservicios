# Proyecto demo: microservicio de Pedidos (.NET 8)
- Clean Architecture estricta: Domain sin dependencias; Application solo referencia Domain
- CQRS con interfaces propias (ICommandHandler/IQueryHandler), NO usar MediatR
- La lógica de negocio vive en las entidades, no en los handlers
- Tests con xUnit; el dominio se testea sin mocks
- Antes de crear cualquier clase nueva, explícame brevemente dónde va y por qué
- Responde siempre en español