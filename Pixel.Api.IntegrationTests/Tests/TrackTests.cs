using System.Net;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Pixel.Contracts.Events;

namespace Pixel.Api.IntegrationTests.Tests;

[TestFixture]
public class TrackTests
{
    [Test]
    public async Task ValidRequest_PixelVisitedEventPublished()
    {
        const string referer = "https://google.com/";
        const string userAgent = "TestUserAgent";
        const string ipAddress = "192.168.1.100";
        
        using var app = new PixelApiTestsApp(collection =>
        {
            collection.AddMassTransitTestHarness();
        });
        var harness = app.Services.GetRequiredService<ITestHarness>();
        var httpClient = app.CreateClient();
        httpClient.DefaultRequestHeaders.Add("Referer", referer);
        httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        httpClient.DefaultRequestHeaders.Add("X-Forwarded-For", ipAddress);

        var response = await httpClient.GetAsync("/track");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pixelVisitedEvents = harness.Published.Select<PixelVisitedEvent>().ToList();
        pixelVisitedEvents.Count.Should().Be(1);
        pixelVisitedEvents.Any(e =>
            e.Context.Message.IpAddress == ipAddress &&
            e.Context.Message.UserAgent == userAgent &&
            e.Context.Message.Referer == referer &&
            e.Context.Message.OccuredOn != DateTime.MinValue)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task ValidRequest_PixelGifReturned()
    {
        using var app = new PixelApiTestsApp(collection =>
        {
            collection.AddMassTransitTestHarness();
        });
        var httpClient = app.CreateClient();
        
        var response = await httpClient.GetAsync("/track");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/gif");
        response.Content.Headers.ContentLength.Should().BeGreaterThan(0);
    }
}

internal class PixelApiTestsApp : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _serviceOverride;

    public PixelApiTestsApp(Action<IServiceCollection> serviceOverride)
    {
        _serviceOverride = serviceOverride;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(_serviceOverride);

        return base.CreateHost(builder);
    }
}