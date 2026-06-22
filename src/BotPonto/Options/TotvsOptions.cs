namespace BotPonto.Options;

public class TotvsOptions
{
    public const string SectionName = "Totvs";

    public string BaseUrl { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public string RestUrl { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Latitude { get; set; } = string.Empty;
    public string Longitude { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Timezone { get; set; } = 180;
}
