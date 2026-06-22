namespace BotPonto.Abstractions;

public interface ITotvsTimesheetService
{
    Task<bool> HasClockingAsync(string token, string direction, CancellationToken cancellationToken = default);
    Task RegisterClockingAsync(string token, CancellationToken cancellationToken = default);
}
