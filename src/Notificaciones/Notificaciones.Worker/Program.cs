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
        cfg.Host(builder.Configuration["Rabbit:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["Rabbit:Usuario"] ?? "admin");
            h.Password(builder.Configuration["Rabbit:Password"] ?? "Demo_Password123!");
        });
        cfg.ConfigureEndpoints(contexto);
    });
});

var host = builder.Build();
host.Run();