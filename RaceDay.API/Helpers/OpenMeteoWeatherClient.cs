using System.Text.Json;

namespace RaceDay.API.Helpers
{
    // Reads the public Open-Meteo forecast. No account or key is required.
    public class OpenMeteoWeatherClient : IWeatherClient
    {
        private readonly HttpClient _http;

        public OpenMeteoWeatherClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<WeatherSnapshot?> GetCurrentAsync(string location, CancellationToken cancellationToken)
        {
            var known = RaceLocations.Find(location);
            string label;
            double latitude;
            double longitude;

            if (known != null)
            {
                label = known.Value.Label;
                latitude = known.Value.Latitude;
                longitude = known.Value.Longitude;
            }
            else
            {
                var geocoded = await GeocodeAsync(location, cancellationToken);
                if (geocoded == null)
                {
                    return null;
                }

                label = geocoded.Value.Label;
                latitude = geocoded.Value.Latitude;
                longitude = geocoded.Value.Longitude;
            }

            string forecastUrl =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                "&current=temperature_2m,weather_code,wind_speed_10m&wind_speed_unit=kmh";

            using var response = await _http.GetAsync(forecastUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("current", out JsonElement current))
            {
                return null;
            }

            return new WeatherSnapshot
            {
                LocationName = label,
                TemperatureC = current.GetProperty("temperature_2m").GetDouble(),
                WindKmh = current.GetProperty("wind_speed_10m").GetDouble(),
                Conditions = Describe(current.GetProperty("weather_code").GetInt32())
            };
        }

        private async Task<(string Label, double Latitude, double Longitude)?> GeocodeAsync(string location, CancellationToken cancellationToken)
        {
            string name = location.Trim();
            if (name.Length > 80)
            {
                name = name[..80];
            }

            string url = "https://geocoding-api.open-meteo.com/v1/search?count=1&name=" + Uri.EscapeDataString(name);
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!document.RootElement.TryGetProperty("results", out JsonElement results) || results.GetArrayLength() == 0)
            {
                return null;
            }

            JsonElement first = results[0];
            string label = first.GetProperty("name").GetString() ?? name;
            return (label, first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
        }

        public static string Describe(int code)
        {
            return code switch
            {
                0 => "Clear",
                1 => "Mainly clear",
                2 => "Partly cloudy",
                3 => "Overcast",
                45 or 48 => "Fog",
                51 or 53 or 55 or 56 or 57 => "Drizzle",
                61 or 63 or 65 or 66 or 67 => "Rain",
                71 or 73 or 75 or 77 => "Snow",
                80 or 81 or 82 => "Rain showers",
                85 or 86 => "Snow showers",
                95 or 96 or 99 => "Thunderstorm",
                _ => "Mixed conditions"
            };
        }
    }
}
