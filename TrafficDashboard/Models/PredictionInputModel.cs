namespace TrafficDashboard.Models
{
    public class PredictionInputModel
    {
        public int Hour { get; set; }
        public int DayOfWeek { get; set; }
        public int IsWeekend { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int NumberOfVehicles { get; set; }

        
        public double PredictedSpeed { get; set; }
        public int CameraId { get; set; }
    }
}