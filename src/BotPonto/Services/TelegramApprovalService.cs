using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BotPonto.Abstractions;
using BotPonto.Models;
using BotPonto.Options;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public class TelegramApprovalService : IClockApprovalService
{
    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramApprovalService> _logger;
    private long _lastUpdateId;

    public TelegramApprovalService(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramApprovalService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> RequestApprovalAsync(string direction, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogWarning("Telegram habilitado sem token. Aprovação negada.");
            return false;
        }

        var chatId = await ResolveChatIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(chatId))
        {
            _logger.LogWarning(
                "ChatId não configurado. Envie /start para o bot e configure Telegram:ChatId no appsettings.");
            return false;
        }

        var label = direction.Equals("entry", StringComparison.OrdinalIgnoreCase) ? "ENTRADA" : "SAÍDA";
        var sentAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await SendMessageAsync(chatId, label, cancellationToken);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ApprovalTimeoutMinutes));

        while (!timeoutCts.IsCancellationRequested)
        {
            var updates = await GetUpdatesAsync(timeoutCts.Token);

            foreach (var update in updates)
            {
                _lastUpdateId = update.UpdateId;

                var approval = TryParseApproval(update, chatId, sentAt);
                if (approval is null) continue;

                if (update.CallbackQuery is not null)
                {
                    await AnswerCallbackAsync(update.CallbackQuery.Id, approval.Value, timeoutCts.Token);
                }

                _logger.LogInformation(
                    "Resposta Telegram recebida: {Approved}",
                    approval.Value ? "SIM" : "NAO");

                return approval.Value;
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), timeoutCts.Token);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        _logger.LogWarning("Tempo esgotado aguardando aprovação no Telegram");
        await SendTextAsync(chatId, "⏱ Tempo esgotado. Ponto não foi registrado.", cancellationToken);
        return false;
    }

    private async Task<string?> ResolveChatIdAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ChatId))
        {
            return _options.ChatId;
        }

        var updates = await GetUpdatesAsync(cancellationToken);
        var latest = updates
            .Select(u => u.Message?.Chat?.Id ?? u.CallbackQuery?.Message?.Chat?.Id)
            .LastOrDefault(id => id.HasValue);

        if (latest is null)
        {
            return null;
        }

        foreach (var update in updates)
        {
            _lastUpdateId = Math.Max(_lastUpdateId, update.UpdateId);
        }

        _logger.LogInformation("ChatId detectado automaticamente: {ChatId}", latest.Value);
        return latest.Value.ToString();
    }

    private async Task SendMessageAsync(string chatId, string label, CancellationToken cancellationToken)
    {
        var payload = new
        {
            chat_id = chatId,
            text = $"🕐 Bater ponto de *{label}*?\n\nResponda SIM ou NAO, ou use os botões abaixo.",
            parse_mode = "Markdown",
            reply_markup = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "✅ SIM", callback_data = "approve_yes" },
                        new { text = "❌ NAO", callback_data = "approve_no" }
                    }
                }
            }
        };

        await PostAsync("sendMessage", payload, cancellationToken);
        _logger.LogInformation("Solicitação de aprovação enviada no Telegram ({Label})", label);
    }

    private async Task SendTextAsync(string chatId, string text, CancellationToken cancellationToken)
    {
        var payload = new { chat_id = chatId, text };
        await PostAsync("sendMessage", payload, cancellationToken);
    }

    private async Task AnswerCallbackAsync(string callbackId, bool approved, CancellationToken cancellationToken)
    {
        var payload = new
        {
            callback_query_id = callbackId,
            text = approved ? "Aprovado!" : "Cancelado."
        };

        await PostAsync("answerCallbackQuery", payload, cancellationToken);
    }

    private async Task PostAsync(string method, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(method, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<TelegramUpdate>> GetUpdatesAsync(CancellationToken cancellationToken)
    {
        var url = $"getUpdates?offset={_lastUpdateId + 1}&timeout=10";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TelegramApiResponse<List<TelegramUpdate>>>(cancellationToken);
        return result?.Result ?? [];
    }

    private static bool? TryParseApproval(TelegramUpdate update, string chatId, long sentAt)
    {
        if (update.CallbackQuery is not null)
        {
            var callbackChatId = update.CallbackQuery.Message?.Chat?.Id.ToString();
            if (callbackChatId != chatId) return null;

            return update.CallbackQuery.Data switch
            {
                "approve_yes" => true,
                "approve_no" => false,
                _ => null
            };
        }

        if (update.Message?.Chat?.Id.ToString() != chatId) return null;
        if (update.Message.Date < sentAt) return null;

        var text = update.Message.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        if (IsYes(text)) return true;
        if (IsNo(text)) return false;

        return null;
    }

    private static bool IsYes(string text)
    {
        return text.Equals("sim", StringComparison.OrdinalIgnoreCase)
            || text.Equals("s", StringComparison.OrdinalIgnoreCase)
            || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || text.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNo(string text)
    {
        return text.Equals("nao", StringComparison.OrdinalIgnoreCase)
            || text.Equals("não", StringComparison.OrdinalIgnoreCase)
            || text.Equals("n", StringComparison.OrdinalIgnoreCase)
            || text.Equals("no", StringComparison.OrdinalIgnoreCase);
    }
}
