namespace Pixel.Contracts.Events;

public record PixelVisitedEvent(string Referer, string UserAgent, string IpAddress, DateTime OccuredOn);