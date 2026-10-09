namespace RaceDay.API.Models
{
    // Maps to the Results table (finish time and position for one enrolment)
    public class Result
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int FinishingPosition { get; set; }
        public DateTime? PublishedAt { get; set; }

        public Enrolment? Enrolment { get; set; }
    }
}
