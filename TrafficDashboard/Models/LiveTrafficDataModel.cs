namespace TrafficDashboard.Models
{
    public class LiveTrafficDataModel
    {
        public int Hour { get; set; }
        public int DayOfWeek { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int NumberOfVehicles { get; set; }
        public int CameraId { get; set; }
        public  double PredictedSpeed { get; set; }
    }
}