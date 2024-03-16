using System.Reflection;
using System.Runtime.CompilerServices;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Pixel.Contracts.Events;

[assembly: InternalsVisibleTo("Pixel.Api.IntegrationTests")]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, configurator) =>
    {
        configurator.Host(builder.Configuration["MassTransit:HostSettings:Uri"],
            (Action<IRabbitMqHostConfigurator>)(host =>
        {
            host.Username(builder.Configuration["MassTransit:HostSettings:Username"]);
            host.Password(builder.Configuration["MassTransit:HostSettings:Password"]);
        }));
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseForwardedHeaders(new() {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
});

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/track", async ([FromServices] IPublishEndpoint publishEndpoint, HttpContext context) =>
{
    var referer = context.Request.Headers["Referer"].ToString();
    var userAgent = context.Request.Headers["User-Agent"].ToString();
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();

    await publishEndpoint.Publish(new PixelVisitedEvent(referer, userAgent, ipAddress, DateTime.UtcNow));

    var pixelGifStream = 
        Assembly.GetExecutingAssembly().GetManifestResourceStream(
            "Pixel.Api.Resources.tracking_pixel.gif");
    return Results.File(pixelGifStream!, "image/gif", "tracking_pixel.gif");
})
.WithOpenApi();

app.Run();