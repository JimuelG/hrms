using Core.Common;

namespace Tests;
public class GetMathTests
{
    [Fact]
    public void Same_Point_Has_Zero_Distance() =>
        Assert.Equal(0, GeoMath.DistanceMeters(15.4755, 120.5963, 15.4755, 120.5963), 6);

    [Fact]
    public void One_Degree_Of_Longitude_At_The_Equator_Is_About_111_Kilometers()
    {
        var d = GeoMath.DistanceMeters(0, 0, 0, 1);
        Assert.InRange(d, 111_100, 111_300);
    }

    [Fact]
    public void Distance_Is_Symmetric()
    {
        var ab = GeoMath.DistanceMeters(15.4755, 120.5963, 14.5995, 120.9842);
        var ba = GeoMath.DistanceMeters(14.5995, 120.9842, 15.4755, 120.5963);
        Assert.Equal(ab, ba, 6);
    }

    [Fact]
    public void Point_About_100m_Away_Is_Inside_A_150m_Radius() =>
        Assert.True(GeoMath.IsWithinRadius(15.4764, 120.5963, 15.4755, 120.5963, 150));

    [Fact]
    public void Point_About_200m_Away_Is_Outside_A_150m_Radius() =>
        Assert.False(GeoMath.IsWithinRadius(15.4773, 120.5963, 15.4755, 120.5963, 150));

    [Fact]
    public void Crossing_The_Antimeridian_Is_Still_A_Short_Distance()
    {
        var d = GeoMath.DistanceMeters(0, 179.9999, 0, -179.9999);
        Assert.InRange(d, 20, 25);
    }
}