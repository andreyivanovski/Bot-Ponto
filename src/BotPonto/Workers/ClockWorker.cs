using BotPonto.Abstractions;
using BotPonto.Options;
using BotPonto.Services;
using Microsoft.Extensions.Options;

namespace BotPonto.Workers;

public class ClockWorker : BackgroundService
{
    private readonly ITotvsAuthService _authService;
    private readonly ITotvsTimesheetService _timesheetService;
    private readonly IClockApprovalService _approvalService;
    private readonly ClockWindowService _windowService;
    private readonly ClockOptions _clockOptions;
    private readonly ILogger<ClockWorker> _logger;

    public ClockWorker(
        ITotvsAuthService authService,
        ITotvsTimesheetService timesheetService,
        IClockApprovalService approvalService,
        ClockWindowService windowService,
        IOptions<ClockOptions> clockOptions,
        ILogger<ClockWorker> logger)
    {
        _authService = authService;
        _timesheetService = timesheetService;
        _approvalService = approvalService;
        _windowService = windowService;
        _clockOptions = clockOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bot Ponto iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            var activeWindow = _windowService.GetActiveWindow();

            if (activeWindow is null)
            {
                var nextWindow = _windowService.GetNextWindow();
                var delay = _windowService.GetDelayUntil(nextWindow.WindowStart);

                _logger.LogInformation(
                    "Aguardando próxima janela ({Type}) em {Start:dddd dd/MM} às {Start:HH:mm}",
                    nextWindow.Type,
                    nextWindow.WindowStart,
                    nextWindow.WindowStart);

                await Task.Delay(delay, stoppingToken);
                continue;
            }

            _logger.LogInformation(
                "Janela ativa: {Type} ({Direction}) até {End:HH:mm}",
                activeWindow.Type,
                activeWindow.Direction,
                activeWindow.WindowEnd);

            await ProcessWindowAsync(activeWindow, stoppingToken);

            var waitUntilEnd = _windowService.GetDelayUntil(activeWindow.WindowEnd);
            if (waitUntilEnd > TimeSpan.Zero)
            {
                await Task.Delay(waitUntilEnd, stoppingToken);
            }
        }
    }

    private async Task ProcessWindowAsync(ClockWindowInfo window, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && _windowService.GetActiveWindow()?.Type == window.Type)
        {
            try
            {
                var token = await _authService.LoginAsync(stoppingToken);

                if (await _timesheetService.HasClockingAsync(token, window.Direction, stoppingToken))
                {
                    _logger.LogInformation("Ponto já batido nesta janela. Encerrando tentativas.");
                    return;
                }

                var approved = await _approvalService.RequestApprovalAsync(window.Direction, stoppingToken);
                if (!approved)
                {
                    _logger.LogWarning("Aprovação negada para batida de {Direction}", window.Direction);
                    return;
                }

                await _timesheetService.RegisterClockingAsync(token, stoppingToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao bater ponto. Nova tentativa em {Seconds}s", _clockOptions.RetryIntervalSeconds);
                await Task.Delay(TimeSpan.FromSeconds(_clockOptions.RetryIntervalSeconds), stoppingToken);
            }
        }
    }
}
