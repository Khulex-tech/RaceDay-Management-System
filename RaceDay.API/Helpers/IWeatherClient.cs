namespace RaceDay.API.Helpers
{
    public class WeatherSnapshot
    {
        public string LocationName { get; set; } = string.Empty;
        public double TemperatureC { get; set; }
        public string Conditions { get; set; } = string.Empty;
        public double WindKmh { get; set; }
    }

    public interface IWeatherClient
    {
        Task<WeatherSnapshot?> GetCurrentAsync(string location, CancellationToken cancellationToken);
    }
}
