namespace OutlookSync.Api;

public class WeatherForecast
{
    public required DateOnly Date { get; init; }

    public required int TemperatureC { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public required string? Summary { get; init; }
}
