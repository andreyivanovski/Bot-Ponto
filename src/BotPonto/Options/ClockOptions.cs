namespace BotPonto.Options;

public class ClockOptions
{
    public const string SectionName = "Clock";

    public string MorningWindowStart { get; set; } = "07:50";
    public string MorningWindowEnd { get; set; } = "08:10";
    public string EveningWindowStart { get; set; } = "16:50";
    public string EveningWindowEnd { get; set; } = "17:10";
    public int RetryIntervalSeconds { get; set; } = 30;
    public string TimeZoneId { get; set; } = "America/Sao_Paulo";
}
