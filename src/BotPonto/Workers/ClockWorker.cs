using BotPonto.Services;

namespace BotPonto.Workers;

public class ClockWorker : BackgroundService
{
    private readonly ClockRunner _clockRunner;
    private readonly ClockWindowService _windowService;
    private readonly ILogger<ClockWorker> _logger;

    public ClockWorker(
        ClockRunner clockRunner,
        ClockWindowService windowService,
        ILogger<ClockWorker> logger)
    {
        _clockRunner = clockRunner;
        _windowService = windowService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = _windowService.GetLocalNowPublic();
        _logger.LogInformation(
            "Bot Ponto iniciado — fuso {TimeZone}, horário local {Now:yyyy-MM-dd HH:mm:ss}",
            _windowService.GetTimeZoneId(),
            now);

        while (!stoppingToken.IsCancellationRequested)
        {
            var activeWindow = _windowService.GetActiveWindow();

            if (activeWindow is not null)
            {
                await ProcessWindowAsync(activeWindow, stoppingToken);
                continue;
            }

            var nextWindow = _windowService.GetNextWindow();
            var delay = _windowService.GetDelayUntil(nextWindow.WindowStart);

            _logger.LogInformation(
                "Aguardando próxima janela ({Type}) em {Start:dddd dd/MM} às {Start:HH:mm} (em {Delay:hh\\:mm\\:ss})",
                nextWindow.Type,
                nextWindow.WindowStart,
                nextWindow.WindowStart,
                delay);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            // Processa a janela que agendamos — não re-detecta (evita pular por skew de relógio/fuso)
            _logger.LogInformation(
                "Disparo agendado: {Type} ({Direction}) — horário local {Now:HH:mm:ss}",
                nextWindow.Type,
                nextWindow.Direction,
                _windowService.GetLocalNowPublic());

            await ProcessWindowAsync(nextWindow, stoppingToken);
        }
    }

    private async Task ProcessWindowAsync(ClockWindowInfo window, CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Janela ativa: {Type} ({Direction}) até {End:HH:mm}",
            window.Type,
            window.Direction,
            window.WindowEnd);

        await _clockRunner.RunOnceAsync(window.Direction, stoppingToken);

        var waitUntilEnd = _windowService.GetDelayUntil(window.WindowEnd);
        if (waitUntilEnd > TimeSpan.Zero)
        {
            await Task.Delay(waitUntilEnd, stoppingToken);
        }
    }
}
