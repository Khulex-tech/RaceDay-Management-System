namespace RaceDay.Integration
{
    public sealed class SqlAndBlobFactAttribute : FactAttribute
    {
        public SqlAndBlobFactAttribute()
        {
            bool required = Environment.GetEnvironmentVariable("RACEDAY_REQUIRE_SQL") == "true";
            if (!required && (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RACEDAY_SQL_CONNECTION")) ||
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RACEDAY_BLOB_CONNECTION"))))
            {
                Skip = "Set RACEDAY_SQL_CONNECTION and RACEDAY_BLOB_CONNECTION to test SQL Server and Azurite.";
            }
        }
    }
}
