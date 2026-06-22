namespace BotPonto.Abstractions;

public interface IClockApprovalService
{
    Task<bool> RequestApprovalAsync(string direction, CancellationToken cancellationToken = default);
}
