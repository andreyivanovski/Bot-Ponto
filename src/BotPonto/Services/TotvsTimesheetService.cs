using System.Net.Http.Headers;
using System.Net.Http.Json;
using BotPonto.Abstractions;
using BotPonto.Models;
using BotPonto.Options;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public class TotvsTimesheetService : ITotvsTimesheetService
{
    private const string CurrentPlaceholder = "{current}";

    private readonly HttpClient _httpClient;
    private readonly TotvsOptions _options;
    private readonly ClockOptions _clockOptions;
    private readonly ILogger<TotvsTimesheetService> _logger;

    public TotvsTimesheetService(
        HttpClient httpClient,
        IOptions<TotvsOptions> options,
        IOptions<ClockOptions> clockOptions,
        ILogger<TotvsTimesheetService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _clockOptions = clockOptions.Value;
        _logger = logger;
    }

    public async Task<bool> HasClockingAsync(string token, string direction, CancellationToken cancellationToken = default)
    {
        var clockings = await GetTodayClockingsAsync(token, cancellationToken);
        var hasClocking = clockings.Any(c =>
            string.Equals(c.Direction?.Trim(), direction, StringComparison.OrdinalIgnoreCase));

        if (hasClocking)
        {
            _logger.LogInformation("Batida '{Direction}' já registrada hoje", direction);
        }

        return hasClocking;
    }

    public async Task RegisterClockingAsync(string token, CancellationToken cancellationToken = default)
    {
        var timeZone = ClockWindowService.ResolveTimeZone(_clockOptions.TimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

        var request = new ClockingRequest
        {
            Latitude = _options.Latitude,
            Longitude = _options.Longitude,
            Timezone = _options.Timezone,
            Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Hour = ToHourMilliseconds(localNow),
            Address = _options.Address
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"timesheet/clockingsGeolocation/{CurrentPlaceholder}");

        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        httpRequest.Content = JsonContent.Create(request);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation(
            "Ponto registrado com sucesso às {Time:HH:mm:ss}",
            localNow);
    }

    private async Task<List<ClockingItem>> GetTodayClockingsAsync(string token, CancellationToken cancellationToken)
    {
        var url = $"timesheet/todayClockings/{CurrentPlaceholder}/{_options.Latitude}/{_options.Longitude}/";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TodayClockingsResponse>(cancellationToken);
        return result?.Items ?? [];
    }

    private static int ToHourMilliseconds(DateTime localTime)
    {
        return (int)localTime.TimeOfDay.TotalMilliseconds;
    }
}
