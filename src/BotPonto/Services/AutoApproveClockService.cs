using BotPonto.Abstractions;

namespace BotPonto.Services;

public class AutoApproveClockService : IClockApprovalService
{
    public Task<bool> RequestApprovalAsync(string direction, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
