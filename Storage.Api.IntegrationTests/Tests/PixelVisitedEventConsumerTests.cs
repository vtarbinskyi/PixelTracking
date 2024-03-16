using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Pixel.Contracts.Events;
using Storage.Api.Consumers;

namespace Storage.Api.IntegrationTests.Tests;

[TestFixture]
public class PixelVisitedEventConsumerTests
{
    [Test]
    public async Task ValidMessage_Consumed()
    {
        const string referer = "https://google.com/";
        const string userAgent = "TestUserAgent";
        const string ipAddress = "192.168.1.100";
        using var app = new StorageApiTestsApp(collection =>
        {
            collection.AddMassTransitTestHarness();
        });
        var harness = app.Services.GetRequiredService<ITestHarness>();
        await harness.Bus.Publish(new PixelVisitedEvent(referer, userAgent, ipAddress, DateTime.UtcNow));

        await harness.Consumed.Any<PixelVisitedEvent>(c => c.Context.Message.IpAddress == ipAddress);
    }

    [TestCaseSource(nameof(TestVisitsSource))]
    public async Task ValidMessage_VisitDataStoredToFile(string referer, string userAgent, string ipAddress)
    {
        var visitTime = DateTime.UtcNow;

        using var app = new StorageApiTestsApp(collection =>
        {
            collection.AddMassTransitTestHarness();
        });
        var config = app.Services.GetRequiredService<IConfiguration>();
        var logFilePath = config["VisitsStorage:StoragePath"];
        
        var harness = app.Services.GetRequiredService<ITestHarness>();
        await harness.Bus.Publish(new PixelVisitedEvent(referer, userAgent, ipAddress, DateTime.UtcNow));
        await harness.Consumed.Any<PixelVisitedEvent>(c => c.Context.Message.IpAddress == ipAddress);

        File.Exists(logFilePath).Should().BeTrue();
        var logContent = await File.ReadAllTextAsync(logFilePath);
        var logEntries = logContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        logEntries.Length.Should().Be(1);
        var logEntry = logEntries.First();
        
        var components = logEntry.Split('|');
        components.Length.Should().Be(4);

        var parsedDateTime = DateTime.Parse(components[0]);
        var parsedReferer = components[1];
        var parsedUserAgent = components[2];
        var parsedIpAddress = components[3];

        parsedDateTime.Should().BeCloseTo(visitTime, precision: TimeSpan.FromSeconds(1));
        parsedReferer.Should().Be(referer.ToStringNullIfEmptyOrNull());
        parsedUserAgent.Should().Be(userAgent.ToStringNullIfEmptyOrNull());
        parsedIpAddress.Should().Be(ipAddress.ToStringNullIfEmptyOrNull());

        File.Delete(logFilePath);
    }

    public static IEnumerable<string[]> TestVisitsSource()
    {
        yield return new[] {"https://google.com/", $"agent-{Guid.NewGuid()}", "192.168.1.100"};
        yield return new[] {null, $"agent-{Guid.NewGuid()}", "192.168.1.100"};
        yield return new[] {"https://google.com", null, "192.168.1.100"};
        yield return new[] {"https://google.com/", $"agent-{Guid.NewGuid()}", null};
    }
}

internal class StorageApiTestsApp : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection> _serviceOverride;

    public StorageApiTestsApp(Action<IServiceCollection> serviceOverride)
    {
        _serviceOverride = serviceOverride;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(_serviceOverride);

        return base.CreateHost(builder);
    }
}