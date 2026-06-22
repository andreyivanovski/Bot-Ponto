using BotPonto.Abstractions;
using BotPonto.Options;
using BotPonto.Services;
using BotPonto.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TotvsOptions>(builder.Configuration.GetSection(TotvsOptions.SectionName));
builder.Services.Configure<ClockOptions>(builder.Configuration.GetSection(ClockOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

builder.Services.AddSingleton<ClockWindowService>();

var telegramOptions = builder.Configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>() ?? new TelegramOptions();
if (telegramOptions.Enabled && !string.IsNullOrWhiteSpace(telegramOptions.BotToken))
{
    builder.Services.AddHttpClient<IClockApprovalService, TelegramApprovalService>((_, client) =>
    {
        client.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramOptions.BotToken}/");
    });
}
else
{
    builder.Services.AddSingleton<IClockApprovalService, AutoApproveClockService>();
}

builder.Services.AddHttpClient<ITotvsAuthService, TotvsAuthService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TotvsOptions>>().Value;
    ConfigureTotvsHttpClient(client, options);
});

builder.Services.AddHttpClient<ITotvsTimesheetService, TotvsTimesheetService>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TotvsOptions>>().Value;
    ConfigureTotvsHttpClient(client, options);
});

builder.Services.AddHostedService<ClockWorker>();

var host = builder.Build();
host.Run();

static void ConfigureTotvsHttpClient(HttpClient client, TotvsOptions options)
{
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
    client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR");
    client.DefaultRequestHeaders.Add("Origin", "https://senffnet148707.protheus.cloudtotvs.com.br:4020");
    client.DefaultRequestHeaders.Add("Referer", "https://senffnet148707.protheus.cloudtotvs.com.br:4020/");
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("x-totvs-app", "0200");
}
