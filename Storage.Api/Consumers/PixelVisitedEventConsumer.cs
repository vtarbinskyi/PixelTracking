using MassTransit;
using Pixel.Contracts.Events;

namespace Storage.Api.Consumers;

public class PixelVisitedEventConsumer : IConsumer<PixelVisitedEvent>
{
    private readonly string _logFilePath;
    private readonly ILogger<PixelVisitedEventConsumer> _logger;
    private static readonly SemaphoreSlim _lock = new(1);

    public PixelVisitedEventConsumer(IConfiguration configuration, ILogger<PixelVisitedEventConsumer> logger)
    {
        _logger = logger;
        _logFilePath = configuration["VisitsStorage:StoragePath"];
    }

    public async Task Consume(ConsumeContext<PixelVisitedEvent> context)
    {
        if (string.IsNullOrEmpty(_logFilePath))
        {
            _logger.LogError($"{nameof(PixelVisitedEventConsumer)} failed due to lack of log file path in configuration.");
            throw new InvalidOperationException();
        }

        var visitData = context.Message;
        var logEntry = GetFormattedLog(visitData.OccuredOn, visitData.Referer, visitData.UserAgent,
            visitData.IpAddress);

        await _lock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_logFilePath, logEntry + Environment.NewLine);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string GetFormattedLog(DateTime occuredOn, string referer, string userAgent, string ipAddress) => $"{occuredOn.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}|{referer.ToStringNullIfEmptyOrNull()}|{userAgent.ToStringNullIfEmptyOrNull()}|{ipAddress.ToStringNullIfEmptyOrNull()}";
}

public static class StringExtensions
{
    public static string ToStringNullIfEmptyOrNull(this string value) => string.IsNullOrEmpty(value) ? "null" : value;
}