namespace BotPonto.Abstractions;

public interface ITotvsAuthService
{
    Task<string> LoginAsync(CancellationToken cancellationToken = default);
}
