using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Stevehead.Numerics;

/// <summary>
/// Represents an unsigned rational data type used in the TIFF format.
/// </summary>
public readonly struct URational :
    IComparable,
    IComparable<URational>,
    IComparisonOperators<URational, URational, bool>,
    IEqualityOperators<URational, URational, bool>,
    IEquatable<URational>
{
    /// <summary>
    /// Gets the <see cref="URational"/> value that represents <c>Infinity</c>.
    /// </summary>
    public static readonly URational Infinity = new(1U, 0U);

    /// <summary>
    /// Gets the <see cref="URational"/> value that represents <c>NaN</c>.
    /// </summary>
    public static readonly URational NaN = new(0U, 0U);

    /// <summary>
    /// Gets the <see cref="URational"/> value that represents <c>1</c>.
    /// </summary>
    public static readonly URational One = new(1U, 1U);

    /// <summary>
    /// Gets the <see cref="URational"/> value that represents <c>0</c>.
    /// </summary>
    public static readonly URational Zero = new(0U, 1U);

    private readonly uint _numerator;
    private readonly uint _denominator;

    // We need this to determine if this instance is the default. We want the default to be 0, not NaN.
    private readonly bool _isConstructed;

    /// <summary>
    /// Initializes a new <see cref="URational"/> instance with the provided numerator and denominator.
    /// </summary>
    /// 
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    public URational(long numerator, long denominator)
    {
        if ((ulong)numerator > uint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(numerator));
        }

        if ((ulong)denominator > uint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator));
        }

        _numerator = (uint)numerator;
        _denominator = (uint)denominator;
        _isConstructed = true;
    }

    // Private constructor
    private URational(uint numerator, uint denominator)
    {
        _numerator = numerator;
        _denominator = denominator;
        _isConstructed = true;
    }

    /// <summary>
    /// Gets the numerator of this instance.
    /// </summary>
    public long Numerator => _numerator;

    /// <summary>
    /// Gets the denominator of this instance.
    /// </summary>
    public long Denominator => _isConstructed ? _denominator : 1L;

    #region Object Overrides
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is URational other && Equals(other);

    public override int GetHashCode()
    {
        Reduce(out uint n, out uint d);
        return HashCode.Combine(n, d);
    }

    public override string ToString() => $"{Numerator} / {Denominator}";
    #endregion

    #region IComparable
    public int CompareTo(object? obj)
    {
        if (obj is URational other)
        {
            return CompareTo(other);
        }

        if (obj == null)
        {
            return 1;
        }

        throw new ArgumentException($"Argument must be of type {nameof(URational)}.");
    }

    public int CompareTo(URational other)
    {
        long n1 = Numerator;
        long d1 = Denominator;
        long n2 = other.Numerator;
        long d2 = other.Denominator;

        if (d1 == 0L)
        {
            if (n1 > 0L) // This is Inf
            {
                if (d2 == 0L && n2 > 0L) return 0; // Other is Inf
                return 1; // Inf is greater than everything but Inf
            }

            // This is NaN
            if (n2 == 0L && d2 == 0) return 0; // Other is NaN
            return -1; // NaN is less than everything but NaN
        }

        // This is a normal fraction

        if (d2 == 0L)
        {
            if (n2 > 0L) return -1; // Other is Inf
            return 1; // Other is NaN
        }

        return (n1 * d2).CompareTo(n2 * d1);
    }
    #endregion

    #region IComparisonOperators
    public static bool operator <(URational left, URational right) => left.CompareTo(right) < 0;

    public static bool operator <=(URational left, URational right) => left.CompareTo(right) <= 0;

    public static bool operator >(URational left, URational right) => left.CompareTo(right) > 0;

    public static bool operator >=(URational left, URational right) => left.CompareTo(right) >= 0;
    #endregion

    #region IEqualityOperators
    public static bool operator ==(URational left, URational right) => left.Equals(right);

    public static bool operator !=(URational left, URational right) => !left.Equals(right);
    #endregion

    #region IEquatable
    public bool Equals(URational other)
    {
        long n1 = Numerator;
        long d1 = Denominator;
        long n2 = other.Numerator;
        long d2 = other.Denominator;

        if (d1 == 0L)
        {
            if (n1 == 0L || d2 != 0L || n2 == 0L)
            {
                return false;
            }

            return n2 > 0L;
        }

        if (n2 == 0L)
        {
            return false;
        }

        return n1 * d2 == n2 * d1;
    }
    #endregion

    #region Internals
    private static uint Gcd(uint a, uint b)
    {
        while (b != 0)
        {
            uint t = b;
            b = a % b;
            a = t;
        }

        return a;
    }

    private void Reduce(out uint numerator, out uint denominator)
    {
        uint n = _numerator;
        uint d = _isConstructed ? _denominator : 1U;

        if (d == 0U)
        {
            numerator = _numerator > 0U ? 1U : 0U;
            denominator = 0U;
        }
        else
        {
            uint gcd = Gcd(n, d);

            numerator = n / gcd;
            denominator = d / gcd;
        }
    }
    #endregion
}
