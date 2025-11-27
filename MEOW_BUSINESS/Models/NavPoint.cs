using System;
using MEOW_BUSINESS.Enums;

namespace MEOW_BUSINESS.Models;

public class NavPoint(float longitude, float latitude, NavPointType type, int id = 0)
{
    public readonly float Longitude = longitude;
    public readonly float Latitude = latitude;

    public readonly NavPointType Type = type;
    public readonly int Id = id;

    /// <summary>
    /// Returns the position of this point in relation to a point of origin (origin) cast to a point on a circle, given in % 
    /// </summary>
    /// <param name="origin">NavPoint of the origin that is in relation to this function</param>
    /// <returns>(int, int) - I.e. (left, bottom)</returns>
    public (int, int) ToScreenPositionFromOrigin(NavPoint origin)
    {
        const int radius = 50; // Radius is 50, Diameter is 100. Treated as percent

        // convert to radians
        double lat1 = origin.Latitude * Math.PI / 180.0;
        double lat2 = Latitude * Math.PI / 180.0;
        double dLon = (Longitude - origin.Longitude) * Math.PI / 180.0;

        // great-circle / atan2(y, x) bearing
        double atY = Math.Sin(dLon) * Math.Cos(lat2);
        double atX = Math.Cos(lat1) * Math.Sin(lat2) -
                     Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        double brng = Math.Atan2(atY, atX);
        brng = brng * 180.0 / Math.PI; // → degrees
        brng = (brng + 360.0) % 360.0; // normalize to 0..360

        double radians = brng * Math.PI / 180.0;
        double x = radius + radius * Math.Cos(radians);
        double y = radius + radius * Math.Sin(radians);

        return ((int)x, (int)y);
    }

    public override string ToString()
    {
        return "Long: " + MathF.Round(Longitude, 4) + "; Lat: " + MathF.Round(Latitude, 4);
    }
}