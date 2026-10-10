namespace Core.Common;
public static class GeoMath
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat /2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
            * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        a = Math.Clamp(a, 0, 1);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static bool IsWithinRadius(
        double lat, double lon, double centerLat, double centerLon, double radiusMeters) =>
        DistanceMeters(lat, lon, centerLat, centerLon) <= radiusMeters;

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}