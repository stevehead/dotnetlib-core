using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Stevehead.Numerics;

/// <summary>
/// Represents a signed rational data type used in the TIFF format.
/// </summary>
public readonly struct Rational :
    IComparable,
    IComparable<Rational>,
    IComparisonOperators<Rational, Rational, bool>,
    IEqualityOperators<Rational, Rational, bool>,
    IEquatable<Rational>
{
    // Required for the edge cases of -int.MinValue / -x.
    private const long Int32MinValueAbs = 0x80000000L;

    /// <summary>
    /// Gets the <see cref="Rational"/> value that represents <c>NaN</c>.
    /// </summary>
    public static readonly Rational NaN = new(0, 0);

    /// <summary>
    /// Gets the <see cref="Rational"/> value that represents <c>Negative Infinity</c>.
    /// </summary>
    public static readonly Rational NegativeInfinity = new(-1, 0);

    /// <summary>
    /// Gets the <see cref="Rational"/> value that represents <c>Positive Infinity</c>.
    /// </summary>
    public static readonly Rational PositiveInfinity = new(1, 0);

    /// <summary>
    /// Gets the <see cref="Rational"/> value that represents <c>1</c>.
    /// </summary>
    public static readonly Rational One = new(1, 1);

    /// <summary>
    /// Gets the <see cref="Rational"/> value that represents <c>0</c>.
    /// </summary>
    public static readonly Rational Zero = new(0, 1);

    private readonly int _numerator;
    private readonly int _denominator;

    // We need this to determine if this instance is the default. We want the default to be 0, not NaN.
    private readonly bool _isConstructed;

    /// <summary>
    /// Initializes a new <see cref="Rational"/> instance with the provided numerator and denominator.
    /// </summary>
    /// 
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    public Rational(int numerator, int denominator)
    {
        _numerator = numerator;
        _denominator = denominator;
        _isConstructed = true;
    }

    /// <summary>
    /// Gets the numerator of this instance.
    /// </summary>
    public int Numerator => _numerator;

    /// <summary>
    /// Gets the denominator of this instance.
    /// </summary>
    public int Denominator => _isConstructed ? _denominator : 1;

    #region Object Overrides
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Rational other && Equals(other);

    public override int GetHashCode()
    {
        Reduce(out int n, out int d);
        return HashCode.Combine(n, d);
    }

    public override string ToString() => $"{Numerator} / {Denominator}";
    #endregion

    #region IComparable
    public int CompareTo(object? obj)
    {
        if (obj is Rational other)
        {
            return CompareTo(other);
        }

        if (obj == null)
        {
            return 1;
        }

        throw new ArgumentException($"Argument must be of type {nameof(Rational)}.");
    }

    public int CompareTo(Rational other)
    {
        long n1 = Numerator;
        long d1 = Denominator;
        long n2 = other.Numerator;
        long d2 = other.Denominator;

        if (d1 == 0L)
        {
            if (n1 > 0L) // This is PosInf
            {
                if (d2 == 0L && n2 > 0L) return 0; // Other is PosInf
                return 1; // PosInf is greater than everything but PosInf
            }

            if (n1 < 0L) // This is NegInf
            {
                if (d2 == 0L)
                {
                    if (n2 > 0L) return -1; // Other is PosInf
                    if (n2 < 0L) return 0; // Other is NegInf
                    return 1; // Other is NaN
                }
            }

            // This is NaN
            if (n2 == 0L && d2 == 0) return 0; // Other is NaN
            return -1; // NaN is less than everything but NaN
        }

        // This is a normal fraction

        if (d2 == 0L)
        {
            if (n2 > 0L) return -1; // Other is PosInf
            return 1; // Other is either NegInf or NaN
        }

        return (n1 * d2).CompareTo(n2 * d1);
    }
    #endregion

    #region IComparisonOperators
    public static bool operator <(Rational left, Rational right) => left.CompareTo(right) < 0;

    public static bool operator <=(Rational left, Rational right) => left.CompareTo(right) <= 0;

    public static bool operator >(Rational left, Rational right) => left.CompareTo(right) > 0;

    public static bool operator >=(Rational left, Rational right) => left.CompareTo(right) >= 0;
    #endregion

    #region IEqualityOperators
    public static bool operator ==(Rational left, Rational right) => left.Equals(right);

    public static bool operator !=(Rational left, Rational right) => !left.Equals(right);
    #endregion

    #region IEquatable
    public bool Equals(Rational other)
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

            if (n1 > 0L)
            {
                return n2 > 0L;
            }

            return n2 < 0L;
        }

        if (n2 == 0L)
        {
            return false;
        }

        return n1 * d2 == n2 * d1;
    }
    #endregion

    #region Internals
    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            long t = b;
            b = a % b;
            a = t;
        }

        return a;
    }

    private void Reduce(out int numerator, out int denominator)
    {
        long n = Numerator;
        long d = Denominator;

        if (d == 0L)
        {
            numerator = n.CompareTo(0L);
            denominator = 0;
        }
        else
        {
            bool isNegative = false;

            if (n < 0L)
            {
                isNegative ^= true;
                n = -n;
            }

            if (d < 0L)
            {
                isNegative ^= true;
                d = -d;
            }

            long gcd = Gcd(d, n);

            n /= gcd;
            d /= gcd;

            if (isNegative)
            {
                numerator = (int)-n;
                denominator = (int)d;
            }
            else if (n == Int32MinValueAbs)
            {
                numerator = int.MinValue;
                denominator = (int)-d;
            }
            else
            {
                numerator = (int)n;
                denominator = (int)d;
            }
        }
    }
    #endregion
}
