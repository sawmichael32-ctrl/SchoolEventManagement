namespace SchoolEventManagement.Models
{
    public class EventAttendanceStat
    {
        public int EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int Registered { get; set; }
        public int CheckedIn { get; set; }
    }

    public class RecentCheckIn
    {
        public string Student { get; set; } = string.Empty;
        public string EventTitle { get; set; } = string.Empty;
        public DateTime Time { get; set; }
        public string Method { get; set; } = string.Empty;
    }
}