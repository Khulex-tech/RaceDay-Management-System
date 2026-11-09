namespace RaceDay.API.Helpers
{
    /*
     * Start locations used by the seeded South African events, plus other towns an organiser might type.
     * Open-Meteo is only called when the location is not in this list.
     */
    public static class RaceLocations
    {
        private static readonly (string Key, string Label, double Latitude, double Longitude)[] Places =
        {
            ("ushaka", "Durban", -29.867, 31.046),
            ("soweto", "Soweto, Johannesburg", -26.268, 27.858),
            ("cape town", "Cape Town", -33.925, 18.424),
            ("johannesburg", "Johannesburg", -26.204, 28.047),
            ("durban", "Durban", -29.858, 31.021),
            ("pretoria", "Pretoria", -25.747, 28.229),
            ("pietermaritzburg", "Pietermaritzburg", -29.601, 30.379),
            ("gqeberha", "Gqeberha", -33.961, 25.620),
            ("port elizabeth", "Gqeberha", -33.961, 25.620),
            ("bloemfontein", "Bloemfontein", -29.085, 26.159),
            ("polokwane", "Polokwane", -23.904, 29.469),
            ("mbombela", "Mbombela", -25.475, 30.970),
            ("nelspruit", "Mbombela", -25.475, 30.970),
            ("east london", "East London", -33.015, 27.912),
            ("kimberley", "Kimberley", -28.728, 24.749),
            ("rustenburg", "Rustenburg", -25.667, 27.242)
        };

        public static (string Label, double Latitude, double Longitude)? Find(string? location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            string lowered = location.ToLowerInvariant();
            foreach (var place in Places)
            {
                if (lowered.Contains(place.Key))
                {
                    return (place.Label, place.Latitude, place.Longitude);
                }
            }

            return null;
        }
    }
}
