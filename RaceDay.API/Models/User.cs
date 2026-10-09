namespace RaceDay.API.Models
{
    /*
     * Maps to the Users table.
     * Organisers and Participants share this table and are split by the Role column.
     */
    public class User
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Events created by this user (only used when the user is an Organiser)
        public List<Event> OrganisedEvents { get; set; } = new();

        // Enrolments made by this user (only used when the user is a Participant)
        public List<Enrolment> Enrolments { get; set; } = new();
    }
}
