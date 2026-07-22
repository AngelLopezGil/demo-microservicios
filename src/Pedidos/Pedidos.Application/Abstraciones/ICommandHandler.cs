namespace Pedidos.Application.Abstracciones;

public interface ICommandHandler<TCommand, TResultado>
{
    Task<TResultado> Handle(TCommand command, CancellationToken cancellationToken);
}