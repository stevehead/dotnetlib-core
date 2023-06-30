using System;
using System.Diagnostics;

namespace Stevehead;

/*
 * THIS IS A WORK IN PROGRESS, NOT TO BE USED JUST YET
 */

/// <summary>
/// Represents a geographical location that is determined by latitude and longitude coordinates.
/// </summary>
public readonly struct GeoCoordinate : IEquatable<GeoCoordinate>
{
    private const double EarthRadius = 6371000;

    private readonly double _latitude;
    private readonly double _longitude;

    /// <summary>
    /// Initializes a new <see cref="GeoCoordinate"/> for the provided latitude and longitude values.
    /// </summary>
    /// 
    /// <param name="latitude">The latitude of the coordinate, in degrees.</param>
    /// <param name="longitude">The longitude of the coordinate, in degrees.</param>
    /// 
    /// <exception cref="ArgumentException"><paramref name="latitude"/> is not a finite <see cref="double"/> value.</exception>
    /// <exception cref="ArgumentException"><paramref name="longitude"/> is not a finite <see cref="double"/> value.</exception>
    public GeoCoordinate(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude))
        {
            throw new ArgumentException("Latitude cannot be NaN or infinity.", nameof(latitude));
        }

        if (double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            throw new ArgumentException("Longitude cannot be NaN or infinity.", nameof(longitude));
        }

        Normalize(latitude, longitude, out _latitude, out _longitude);
    }

    // Internal constructor to bypass argument checking.
    private GeoCoordinate(double latitude, double longitude, bool isInternal)
    {
        Debug.Assert(isInternal);
        Normalize(latitude, longitude, out _latitude, out _longitude);
    }

    /// <summary>
    /// Determines if this <see cref="GeoCoordinate"/> is in the Eastern Hemisphere.
    /// </summary>
    public bool IsEasternHemisphere
        => _longitude > 0;

    /// <summary>
    /// Determines if this <see cref="GeoCoordinate"/> is in the Northern Hemisphere.
    /// </summary>
    public bool IsNorthernHemisphere
        => _latitude > 0;

    /// <summary>
    /// Determines if this <see cref="GeoCoordinate"/> is in the Southern Hemisphere.
    /// </summary>
    public bool IsSoutherHemisphere
        => _latitude < 0;

    /// <summary>
    /// Determines if this <see cref="GeoCoordinate"/> is in the Western Hemisphere.
    /// </summary>
    public bool IsWesternHemisphere
        => _longitude < 0;

    /// <summary>
    /// The latitude of this <see cref="GeoCoordinate"/>, in degrees.
    /// </summary>
    public double Latitude => _latitude;

    /// <summary>
    /// The longitude of this <see cref="GeoCoordinate"/>, in degrees.
    /// </summary>
    public double Longitude => _longitude;

    /// <summary>
    /// Determines the latitude of the this <see cref="GeoCoordinate"/>, in terms of degrees, minutes, and seconds.
    /// </summary>
    /// 
    /// <return>A tuple of the degrees, minutes, and seconds.</return>
    public (int Degrees, int Minutes, double Seconds) DeconstructLatitude()
        => Deconstruct(_latitude);

    /// <summary>
    /// Determines the longitude of this <see cref="GeoCoordinate"/>, in terms of degrees, minutes, and seconds.
    /// </summary>
    /// 
    /// <return>A tuple of the degrees, minutes, and seconds.</return>
    public (int Degrees, int Minutes, double Seconds) DeconstructLongitude()
        => Deconstruct(_longitude);

    /// <summary>
    /// Determines and returns the <see cref="GeoCoordinate"/> exactly opposite of this <see cref="GeoCoordinate"/> on
    /// the other side of the planet.
    /// </summary>
    /// 
    /// <returns>The antipode to this value.</returns>
    public GeoCoordinate GetAntipode()
    {
        double latitude = -_latitude;
        double longitude = _longitude - 180;
        return new GeoCoordinate(latitude, longitude, true);
    }

    public override bool Equals(object? obj) => obj is GeoCoordinate other && Equals(other);

    public bool Equals(GeoCoordinate other) => _latitude == other._latitude && _longitude == other._longitude;

    public override int GetHashCode() => HashCode.Combine(_latitude, _longitude);

    public override string ToString()
    {
        (int latDegrees, int latMinutes, double latSeconds) = DeconstructLatitude();
        (int lonDegrees, int lonMinutes, double lonSeconds) = DeconstructLongitude();

        string latRef, lonRef;

        if (latDegrees < 0)
        {
            latRef = "S";
            latDegrees *= -1;
        }
        else
        {
            latRef = "N";
        }

        if (lonDegrees < 0)
        {
            lonRef = "W";
            lonDegrees *= -1;
        }
        else
        {
            lonRef = "E";
        }

        return $"{latDegrees}\u00B0{latMinutes}'{(int)latSeconds}\"{latRef}, {lonDegrees}\u00B0{lonMinutes}'{(int)lonSeconds}\"{lonRef}";
    }

    /// <summary>
    /// Determines the approximate distance, in meters, between the two provided <see cref="GeoCoordinate"/> values
    /// along the curvature of the Earth.
    /// </summary>
    /// 
    /// <param name="x">The first <see cref="GeoCoordinate"/>.</param>
    /// <param name="y">The second <see cref="GeoCoordinate"/>.</param>
    /// 
    /// <returns>The approximate distance between the two <see cref="GeoCoordinate"/> values, in meters.</returns>
    public static double DistanceBetween(GeoCoordinate x, GeoCoordinate y) => InternalDistanceBetween(x, y, EarthRadius);

    /// <summary>
    /// Determines the approximate distance between the two provided <see cref="GeoCoordinate"/> values along the
    /// curvature of a sphere.
    /// </summary>
    /// 
    /// <param name="x">The first <see cref="GeoCoordinate"/>.</param>
    /// <param name="y">The second <see cref="GeoCoordinate"/>.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// 
    /// <returns>The approximate distance between the two <see cref="GeoCoordinate"/> values.</returns>
    /// 
    /// <exception cref="ArgumentException"><paramref name="radius"/> is not a finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radius"/> is less than 0.</exception>
    public static double DistanceBetween(GeoCoordinate x, GeoCoordinate y, double radius)
    {
        if (!double.IsFinite(radius))
        {
            throw new ArgumentException("Radius cannot be NaN or Infinity.", nameof(radius));
        }

        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius cannot be less than zero.");
        }

        if (radius == 0) return 0;

        return InternalDistanceBetween(x, y, radius);
    }

    private static double InternalDistanceBetween(GeoCoordinate x, GeoCoordinate y, double radius)
    {
        double o1 = x._latitude * Math.PI / 180;
        double o2 = y._latitude * Math.PI / 180;
        double deltaO = (y._latitude - x._latitude) * Math.PI / 180;
        double deltaL = (y._longitude - x._longitude) * Math.PI / 180;

        double a = Math.Pow(Math.Sin(deltaO / 2), 2) + Math.Cos(o1) * Math.Cos(o2) * Math.Pow(Math.Sin(deltaL / 2), 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return radius * c;
    }

    public static bool operator ==(GeoCoordinate x, GeoCoordinate y) => x.Equals(y);

    public static bool operator !=(GeoCoordinate x, GeoCoordinate y) => !x.Equals(y);

    private static (int Degrees, int Minutes, double Seconds) Deconstruct(double value)
    {
        bool isNegative;

        if (value < 0)
        {
            value = -value;
            isNegative = true;
        }
        else
        {
            isNegative = false;
        }

        int degrees = (int)value;

        value -= degrees;

        int minutes = (int)(value * 60);

        value -= minutes / 60.0;

        double seconds = value * 3600;

        if (isNegative)
        {
            degrees *= -1;
        }

        return (degrees, minutes, seconds);
    }

    private static void Normalize(double rawLatitude, double rawLongitude, out double latitude, out double longitude)
    {
        int quadrant = ((int)Math.Floor(Math.Abs(rawLatitude) / 90)) % 4;
        double pole = rawLatitude > 0 ? 90 : -90;
        double offset = rawLatitude % 90;

        switch (quadrant)
        {
            case 0:
                rawLatitude = offset;
                break;
            case 1:
                rawLatitude = pole - offset;
                rawLongitude += 180;
                break;
            case 2:
                rawLatitude = -offset;
                rawLongitude += 180;
                break;
            case 3:
                rawLatitude = -pole + offset;
                break;
        }

        if (rawLatitude == 90 || rawLatitude == -90)
        {
            rawLongitude = 0;
        }
        else if (rawLongitude < -180 || rawLongitude > 180)
        {
            rawLongitude -= Math.Floor((rawLongitude + 180) / 360) * 360;
        }

        latitude = rawLatitude;
        longitude = rawLongitude;
    }
}
