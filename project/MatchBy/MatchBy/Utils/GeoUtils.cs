namespace MatchBy.Utils;

public static class GeoUtils
{
    private const double EarthRadius = 6371;
    /// <summary>
    /// Calculates the distance between two geographic coordinates using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of the first point in degrees.</param>
    /// <param name="lon1">Longitude of the first point in degrees.</param>
    /// <param name="lat2">Latitude of the second point in degrees.</param>
    /// <param name="lon2">Longitude of the second point in degrees.</param>
    /// <returns>
    /// The distance between the two points in kilometers.
    /// </returns>
    public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadius * c;
    }
}