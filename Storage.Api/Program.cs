using System.Runtime.CompilerServices;
using MassTransit;
using Storage.Api.Consumers;

[assembly: InternalsVisibleTo("Storage.Api.IntegrationTests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PixelVisitedEventConsumer>();

    x.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(builder.Configuration["MassTransit:HostSettings:Uri"],
            (Action<IRabbitMqHostConfigurator>)(host =>
            {
                host.Username(builder.Configuration["MassTransit:HostSettings:Username"]);
                host.Password(builder.Configuration["MassTransit:HostSettings:Password"]);
            }));
        
        configurator.ReceiveEndpoint("visit-data-events", e =>
        {
            e.ConfigureConsumer<PixelVisitedEventConsumer>(context);
        });
    });
});

var app = builder.Build();

app.Run();