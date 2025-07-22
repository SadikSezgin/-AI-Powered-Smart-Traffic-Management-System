using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TrafficDashboard.Models;

namespace TrafficDashboard.Services
{
    public class PredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "http://localhost:5006/predict";

        public PredictionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<double> GetPredictedSpeedAsync(PredictionInputModel input)
        {
            var requestBody = new
            {   
                hour = input.Hour,
                dayOfWeek = input.DayOfWeek,
                isWeekend = input.IsWeekend,
                latitude = input.Latitude,
                longitude = input.Longitude,
                numberOfVehicles = input.NumberOfVehicles
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await _httpClient.PostAsync(_apiUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var result = await JsonSerializer.DeserializeAsync<PredictionResponse>(stream);
                    return result?.PredictedSpeed ?? 0;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        private class PredictionResponse
        {
            [JsonPropertyName("PredictedSpeed")]
            public double PredictedSpeed { get; set; }
        }
    }
}