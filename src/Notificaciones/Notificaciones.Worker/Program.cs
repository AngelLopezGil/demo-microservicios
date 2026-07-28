using MassTransit;
using Notificaciones.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<EmisorEmails>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<NotificarConfirmacionConsumer>();
    x.AddConsumer<NotificarCancelacionConsumer>();

    x.UsingRabbitMq((contexto, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("admin");
            h.Password("Demo_Password123!");
        });
        cfg.ConfigureEndpoints(contexto);
    });
});

var host = builder.Build();
host.Run();