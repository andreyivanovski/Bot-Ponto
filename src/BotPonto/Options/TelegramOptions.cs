namespace BotPonto.Options;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public bool Enabled { get; set; } = true;
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public int ApprovalTimeoutMinutes { get; set; } = 15;
    public int PollIntervalSeconds { get; set; } = 3;
}
