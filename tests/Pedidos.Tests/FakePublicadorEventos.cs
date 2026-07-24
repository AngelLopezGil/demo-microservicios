using Pedidos.Application.Abstracciones;

namespace Pedidos.Tests;

public class FakePublicadorEventos : IPublicadorEventos
{
    public List<object> Publicados { get; } = new();

    public Task Publicar<TEvento>(TEvento evento, CancellationToken ct)
        where TEvento : class
    {
        Publicados.Add(evento);
        return Task.CompletedTask;
    }
}