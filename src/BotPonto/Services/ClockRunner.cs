using BotPonto.Abstractions;
using BotPonto.Options;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public class ClockRunner
{
    private readonly ITotvsAuthService _authService;
    private readonly ITotvsTimesheetService _timesheetService;
    private readonly IClockApprovalService _approvalService;
    private readonly ITelegramNotifier _notifier;
    private readonly ClockOptions _clockOptions;
    private readonly ILogger<ClockRunner> _logger;

    public ClockRunner(
        ITotvsAuthService authService,
        ITotvsTimesheetService timesheetService,
        IClockApprovalService approvalService,
        ITelegramNotifier notifier,
        IOptions<ClockOptions> clockOptions,
        ILogger<ClockRunner> logger)
    {
        _authService = authService;
        _timesheetService = timesheetService;
        _approvalService = approvalService;
        _notifier = notifier;
        _clockOptions = clockOptions.Value;
        _logger = logger;
    }

    public async Task RunOnceAsync(string direction, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Execução única iniciada para direção: {Direction}", direction);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(20));

        var approved = false;
        Exception? lastError = null;

        while (!timeoutCts.IsCancellationRequested)
        {
            try
            {
                var token = await _authService.LoginAsync(timeoutCts.Token);

                if (await _timesheetService.HasClockingAsync(token, direction, timeoutCts.Token))
                {
                    _logger.LogInformation("Ponto já batido hoje ({Direction}). Encerrando.", direction);
                    return;
                }

                if (!approved)
                {
                    approved = await _approvalService.RequestApprovalAsync(direction, timeoutCts.Token);
                    if (!approved)
                    {
                        _logger.LogWarning("Aprovação negada para batida de {Direction}", direction);
                        return;
                    }
                }

                await _timesheetService.RegisterClockingAsync(token, timeoutCts.Token);
                await _notifier.NotifySuccessAsync(direction, timeoutCts.Token);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastError = ex;
                _logger.LogError(ex, "Falha ao bater ponto. Nova tentativa em {Seconds}s", _clockOptions.RetryIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(_clockOptions.RetryIntervalSeconds), timeoutCts.Token);
            }
        }

        if (approved && lastError is not null)
        {
            await _notifier.NotifyErrorAsync(direction, lastError.Message, CancellationToken.None);
        }
    }
}
