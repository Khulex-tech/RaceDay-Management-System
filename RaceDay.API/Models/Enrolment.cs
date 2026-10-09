namespace RaceDay.API.Models
{
    /*
     * Maps to the Enrolments table.
     * This is the junction table that links a Participant, an Event and the chosen Category.
     */
    public class Enrolment
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public string EnrolmentStatus { get; set; } = string.Empty;
        public DateTime EnrolledAt { get; set; }

        public User? Participant { get; set; }
        public Event? Event { get; set; }
        public Category? Category { get; set; }

        // An enrolment can have at most one result
        public Result? Result { get; set; }
    }
}
