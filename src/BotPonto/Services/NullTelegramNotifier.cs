using BotPonto.Abstractions;

namespace BotPonto.Services;

public class NullTelegramNotifier : ITelegramNotifier
{
    public Task NotifySuccessAsync(string direction, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task NotifyErrorAsync(string direction, string errorMessage, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
