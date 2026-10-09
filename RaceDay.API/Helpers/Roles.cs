namespace RaceDay.API.Helpers
{
    // Fixed role and status values so we never mistype them in the controllers
    public static class Roles
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }

    public static class EnrolmentStatuses
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Confirmed, Cancelled };
    }
}
