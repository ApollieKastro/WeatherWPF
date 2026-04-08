using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WeatherWPF
{
    public class ApiLocation
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class ApiCondition
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }

    public class ApiCurrent
    {
        [JsonPropertyName("temp_c")]
        public double TempC { get; set; }

        [JsonPropertyName("feelslike_c")]
        public double FeelsLikeC { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("wind_kph")]
        public double WindKph { get; set; }

        [JsonPropertyName("pressure_mb")]
        public double PressureMb { get; set; }

        [JsonPropertyName("condition")]
        public ApiCondition Condition { get; set; }
    }

    public class ApiForecastDay
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("day")]
        public ApiForecastDayDetail Day { get; set; }
    }

    public class ApiForecastDayDetail
    {
        [JsonPropertyName("maxtemp_c")]
        public double MaxTempC { get; set; }

        [JsonPropertyName("mintemp_c")]
        public double MinTempC { get; set; }

        [JsonPropertyName("condition")]
        public ApiCondition Condition { get; set; }
    }

    public class ApiForecast
    {
        [JsonPropertyName("forecastday")]
        public List<ApiForecastDay> ForecastDay { get; set; }
    }

    public class ApiResponse
    {
        [JsonPropertyName("location")]
        public ApiLocation Location { get; set; }

        [JsonPropertyName("current")]
        public ApiCurrent Current { get; set; }

        [JsonPropertyName("forecast")]
        public ApiForecast Forecast { get; set; }
    }

    public class ApiErrorDetails
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class ApiErrorResponse
    {
        [JsonPropertyName("error")]
        public ApiErrorDetails Error { get; set; }
    }

    public class AppSettings
    {
        public string LastCity { get; set; } = "";
        public List<string> Favorites { get; set; } = new List<string>();
        public List<string> History { get; set; } = new List<string>();
    }
}