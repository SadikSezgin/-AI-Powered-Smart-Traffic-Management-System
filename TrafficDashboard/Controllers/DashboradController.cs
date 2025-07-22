using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TrafficDashboard.Models;
using TrafficDashboard.Services;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace TrafficDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly PredictionService _predictionService;

        private static ConcurrentDictionary<int, double> _cameraSpeeds = new ConcurrentDictionary<int, double>();
        private static ConcurrentDictionary<int, string> _cameraSuggestions = new ConcurrentDictionary<int, string>();

        private static ConcurrentQueue<int> recentVehicleCounts = new ConcurrentQueue<int>();
        private const int maxWindow = 10;

        private static int latestVehicleCount = 0;
        private static double latestPredictedSpeed = 0.0;
        private static string latestLiveSignalSuggestion = "Deafult signal timing";
        

        public DashboardController(PredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        // GET: /Dashboard/Index
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.LatestLiveSignalSuggestion = latestLiveSignalSuggestion;
            ViewBag.LatestVehicleCount = latestVehicleCount;
            ViewBag.LatestPredictedSpeed = latestPredictedSpeed;

            ViewBag.CameraSpeeds = _cameraSpeeds;
            ViewBag.CameraSuggestions = _cameraSuggestions;

            return View();
        }

        // POST: /Dashboard/Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PredictionInputModel input)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Status = "Model binding failed.";
                return View(input);
            }

            // Determine if its weekend
            input.IsWeekend = (input.DayOfWeek == 5 || input.DayOfWeek == 6) ? 1 : 0;

            double predictedSpeed = await _predictionService.GetPredictedSpeedAsync(input);
            ViewBag.PredictedSpeed = predictedSpeed;
            ViewBag.Status = "Prediction successful.";

            if (recentVehicleCounts.Count >= maxWindow)
                recentVehicleCounts.TryDequeue(out _);
            
            recentVehicleCounts.Enqueue(input.NumberOfVehicles);

            bool allAbove50 = recentVehicleCounts.Count == maxWindow && recentVehicleCounts.All(v => v > 15);
            bool allAbove30 = recentVehicleCounts.Count == maxWindow && recentVehicleCounts.All(v => v > 8);

            if (allAbove50)
                ViewBag.SignalSuggestion = "Extend green light by 15 seconds (high sustained traffic)";
            else if (allAbove30)
                ViewBag.SignalSuggestion = "Extend green light by 5 seconds (moderate sustained traffic)";
            else
                ViewBag.SignalSuggestion = "Default signal timing";

            ViewBag.LatestLiveSignalSuggestion = latestLiveSignalSuggestion;
            ViewBag.LatestVehicleCount = latestVehicleCount;
            ViewBag.LatestPredictedSpeed = latestPredictedSpeed;

            ViewBag.CameraSpeeds = _cameraSpeeds;
            ViewBag.CameraSuggestions = _cameraSuggestions;

            return View(input);
        }


        // POST: /Dashboard/Index/ReceiveTrafficData
        [HttpPost("Dashboard/Index/ReceiveTrafficData")]
        public Task<IActionResult> ReceiveTrafficData([FromBody] LiveTrafficDataModel data)
        {
            try
            {
                var input = new PredictionInputModel
                {
                    Hour = data.Hour,
                    DayOfWeek = data.DayOfWeek,
                    Latitude = data.Latitude,
                    Longitude = data.Longitude,
                    NumberOfVehicles = data.NumberOfVehicles,
                    IsWeekend = (data.DayOfWeek == 5 || data.DayOfWeek == 6) ? 1 : 0
                };

                int cameraId = data.CameraId;
                latestVehicleCount = data.NumberOfVehicles;
                double predictedSpeed = data.PredictedSpeed;

                _cameraSpeeds[cameraId] = predictedSpeed;
                
                // Determine suggestion
                string suggestion;
                if (data.NumberOfVehicles > 15 && predictedSpeed < 20)
                    suggestion = "Extend green light by 10 seconds (live detection)";
                else if (data.NumberOfVehicles > 6 && predictedSpeed < 26)
                    suggestion = "Extend green light by 5 seconds (live detection)";
                else
                    suggestion = "Default signal timing (live detection)";

                _cameraSuggestions[cameraId] = suggestion;

                latestPredictedSpeed = predictedSpeed;
                latestLiveSignalSuggestion = suggestion;

                return Task.FromResult<IActionResult>(Ok(new {
                latestPredictedSpeed,
                latestLiveSignalSuggestion,
                latestVehicleCount
                }));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ReceiveTrafficData Error: {ex.Message}");
                return Task.FromResult<IActionResult>(
                    StatusCode(500, $"LiveDetectionError: {ex.Message}")
                );

            }
        }


        [HttpGet("Dashboard/GetLatestTrafficData")]
        public IActionResult GetLatestTrafficData()
        {
            return Ok(new 
            { 
                latestVehicleCount,
                latestPredictedSpeed,
                latestLiveSignalSuggestion,
                cameraSpeeds = _cameraSpeeds,
                cameraSuggestions = _cameraSuggestions
            });
        }
    }
}
