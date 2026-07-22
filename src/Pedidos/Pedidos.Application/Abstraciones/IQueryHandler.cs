namespace Pedidos.Application.Abstracciones;

public interface IQueryHandler<TQuery, TResultado>
{
    Task<TResultado> Handle(TQuery query, CancellationToken cancellationToken);
}