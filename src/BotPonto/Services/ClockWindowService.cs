using BotPonto.Options;
using Microsoft.Extensions.Options;

namespace BotPonto.Services;

public enum ClockWindowType
{
    None,
    Morning,
    Evening
}

public record ClockWindowInfo(
    ClockWindowType Type,
    string Direction,
    DateTime WindowStart,
    DateTime WindowEnd);

public class ClockWindowService
{
    private readonly ClockOptions _options;
    private readonly TimeZoneInfo _timeZone;

    public ClockWindowService(Microsoft.Extensions.Options.IOptions<ClockOptions> options)
    {
        _options = options.Value;
        _timeZone = ResolveTimeZone(_options.TimeZoneId);
    }

    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    public ClockWindowInfo? GetActiveWindow()
    {
        var now = GetLocalNow();
        if (!IsWeekday(now.Date))
        {
            return null;
        }

        var morning = BuildWindow(now, _options.MorningWindowStart, _options.MorningWindowEnd, ClockWindowType.Morning, "entry");
        var evening = BuildWindow(now, _options.EveningWindowStart, _options.EveningWindowEnd, ClockWindowType.Evening, "exit");

        if (morning is not null) return morning;
        if (evening is not null) return evening;
        return null;
    }

    public ClockWindowInfo GetNextWindow()
    {
        var now = GetLocalNow();

        if (IsWeekday(now.Date))
        {
            var todayMorning = CreateWindowBounds(now.Date, _options.MorningWindowStart, _options.MorningWindowEnd);
            if (now < todayMorning.Start)
            {
                return new ClockWindowInfo(ClockWindowType.Morning, "entry", todayMorning.Start, todayMorning.End);
            }

            var todayEvening = CreateWindowBounds(now.Date, _options.EveningWindowStart, _options.EveningWindowEnd);
            if (now < todayEvening.Start)
            {
                return new ClockWindowInfo(ClockWindowType.Evening, "exit", todayEvening.Start, todayEvening.End);
            }
        }

        var nextWeekday = GetNextWeekday(now.Date.AddDays(1));
        var nextMorning = CreateWindowBounds(nextWeekday, _options.MorningWindowStart, _options.MorningWindowEnd);
        return new ClockWindowInfo(ClockWindowType.Morning, "entry", nextMorning.Start, nextMorning.End);
    }

    public TimeSpan GetDelayUntil(DateTime targetLocal)
    {
        var now = GetLocalNow();
        var delay = targetLocal - now;
        return delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
    }

    public bool IsWithinWindow(ClockWindowInfo window)
    {
        var now = GetLocalNow();
        return now >= window.WindowStart && now < window.WindowEnd;
    }

    public DateTime GetLocalNowPublic() => GetLocalNow();

    public string GetTimeZoneId() => _timeZone.Id;

    private DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }

    private ClockWindowInfo? BuildWindow(DateTime now, string start, string end, ClockWindowType type, string direction)
    {
        var bounds = CreateWindowBounds(now.Date, start, end);
        if (now >= bounds.Start && now < bounds.End)
        {
            return new ClockWindowInfo(type, direction, bounds.Start, bounds.End);
        }

        return null;
    }

    private static bool IsWeekday(DateTime date)
    {
        return date.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
    }

    private static DateTime GetNextWeekday(DateTime date)
    {
        while (!IsWeekday(date))
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static (DateTime Start, DateTime End) CreateWindowBounds(DateTime date, string start, string end)
    {
        var startTime = TimeOnly.Parse(start);
        var endTime = TimeOnly.Parse(end);
        var windowStart = date.Add(startTime.ToTimeSpan());
        var windowEnd = date.Add(endTime.ToTimeSpan());
        return (windowStart, windowEnd);
    }
}
