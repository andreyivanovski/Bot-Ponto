using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BotPonto.Abstractions;
using BotPonto.Models;
using BotPonto.Options;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public partial class TotvsAuthService : ITotvsAuthService
{
    private readonly HttpClient _httpClient;
    private readonly TotvsOptions _options;
    private readonly ILogger<TotvsAuthService> _logger;

    [GeneratedRegex(@"token=([^&""]+)")]
    private static partial Regex TokenRegex();

    public TotvsAuthService(
        HttpClient httpClient,
        IOptions<TotvsOptions> options,
        ILogger<TotvsAuthService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> LoginAsync(CancellationToken cancellationToken = default)
    {
        var request = new LoginRequest
        {
            User = _options.User,
            Password = _options.Password,
            RedirectUrl = _options.RedirectUrl,
            RestUrl = _options.RestUrl
        };

        var response = await _httpClient.PostAsJsonAsync("auth/login", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = TokenRegex().Match(html);

        if (!match.Success)
        {
            _logger.LogError("Token não encontrado na resposta de login");
            throw new InvalidOperationException("Não foi possível extrair o token da resposta de login.");
        }

        var token = match.Groups[1].Value;
        _logger.LogInformation("Login realizado com sucesso");
        return token;
    }
}
