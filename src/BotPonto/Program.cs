using BotPonto.Abstractions;
using BotPonto.Options;
using BotPonto.Services;
using BotPonto.Workers;

var onceMode = args.Contains("--once");
var directionArg = args.FirstOrDefault(a => a.StartsWith("--direction=", StringComparison.OrdinalIgnoreCase))
    ?.Split('=', 2)[1];

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<TotvsOptions>(builder.Configuration.GetSection(TotvsOptions.SectionName));
builder.Services.Configure<ClockOptions>(builder.Configuration.GetSection(ClockOptions.SectionName));
builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection(TelegramOptions.SectionName));

builder.Services.AddSingleton<ClockWindowService>();
builder.Services.AddSingleton<ClockRunner>();

var telegramOptions = builder.Configuration.GetSection(TelegramOptions.SectionName).Get<TelegramOptions>() ?? new TelegramOptions();
if (telegramOptions.Enabled && !string.IsNullOrWhiteSpace(telegramOptions.BotToken))
{
    builder.Services.AddHttpClient<IClockApprovalService, TelegramApprovalService>((_, client) =>
    {
        client.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramOptions.BotToken}/");
    });
    builder.Services.AddHttpClient<ITelegramNotifier, TelegramNotifierService>((_, client) =>
    {
        client.BaseAddress = new Uri($"https://api.telegram.org/bot{telegramOptions.BotToken}/");
    });
}
else
{
    builder.Services.AddSingleton<IClockApprovalService, AutoApproveClockService>();
    builder.Services.AddSingleton<ITelegramNotifier, NullTelegramNotifier>();
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

if (onceMode)
{
    if (string.IsNullOrWhiteSpace(directionArg) ||
        !directionArg.Equals("entry", StringComparison.OrdinalIgnoreCase) &&
        !directionArg.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Uso: dotnet run -- --once --direction=entry|exit");
        return 1;
    }

    var host = builder.Build();
    var runner = host.Services.GetRequiredService<ClockRunner>();
    await runner.RunOnceAsync(directionArg.ToLowerInvariant());
    return 0;
}

builder.Services.AddHostedService<ClockWorker>();

var workerHost = builder.Build();
await workerHost.RunAsync();
return 0;

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
