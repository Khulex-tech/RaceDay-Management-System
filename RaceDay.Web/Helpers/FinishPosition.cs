namespace RaceDay.Web.Helpers
{
    public static class FinishPosition
    {
        // 1st, 2nd, 3rd, 4th, with the 11/12/13 exception.
        public static string Format(int position, int totalFinishers)
        {
            string suffix = "th";
            int lastTwo = position % 100;
            if (lastTwo < 11 || lastTwo > 13)
            {
                suffix = (position % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th"
                };
            }

            // totalFinishers is how many results have been saved, not the size of the race.
            string recorded = totalFinishers == 1 ? "1 result on file" : $"{totalFinishers} results on file";
            return $"{position}{suffix} recorded. {recorded}.";
        }
    }
}
