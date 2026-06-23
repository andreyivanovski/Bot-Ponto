namespace BotPonto.Abstractions;

public interface ITelegramNotifier
{
    Task NotifySuccessAsync(string direction, CancellationToken cancellationToken = default);
    Task NotifyErrorAsync(string direction, string errorMessage, CancellationToken cancellationToken = default);
}
