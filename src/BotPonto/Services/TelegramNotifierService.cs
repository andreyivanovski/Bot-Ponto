using System.Text;
using System.Text.Json;
using BotPonto.Abstractions;
using BotPonto.Options;
using BotPonto.Services;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public class TelegramNotifierService : ITelegramNotifier
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ClockOptions _clockOptions;
    private readonly ILogger<TelegramNotifierService> _logger;

    public TelegramNotifierService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        IOptions<ClockOptions> clockOptions,
        ILogger<TelegramNotifierService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _clockOptions = clockOptions.Value;
        _logger = logger;
    }

    public Task NotifySuccessAsync(string direction, CancellationToken cancellationToken = default)
    {
        var label = GetLabel(direction);
        var timeZone = ClockWindowService.ResolveTimeZone(_clockOptions.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var message = $"✅ Ponto de *{label}* registrado com sucesso às {localNow:HH:mm:ss}!";
        return SendAsync(message, cancellationToken);
    }

    public Task NotifyErrorAsync(string direction, string errorMessage, CancellationToken cancellationToken = default)
    {
        var label = GetLabel(direction);
        var message = $"❌ Erro ao registrar ponto de *{label}*:\n{errorMessage}";
        return SendAsync(message, cancellationToken);
    }

    private async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(_options.ChatId))
        {
            return;
        }

        var payload = new
        {
            chat_id = _options.ChatId,
            text,
            parse_mode = "Markdown"
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("sendMessage", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Notificação enviada no Telegram");
    }

    private static string GetLabel(string direction)
    {
        return direction.Equals("entry", StringComparison.OrdinalIgnoreCase) ? "ENTRADA" : "SAÍDA";
    }
}
