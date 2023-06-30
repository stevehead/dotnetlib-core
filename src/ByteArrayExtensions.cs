using System;
using System.Text;

namespace Stevehead;

/// <summary>
/// Extension methods for byte arrays.
/// </summary>
public static class ByteArrayExtensions
{
    /// <summary>
    /// Creates a hexadecimal string of the provided byte array.
    /// </summary>
    /// 
    /// <param name="bytes">The byte array to be used.</param>
    /// <param name="upperCase">Determines if the output string should be uppercase or lowercase.</param>
    /// <param name="delimiter">A string that is inserted between each byte.</param>
    /// <returns>A string that is the hexadecimal representation of the provided byte array.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <c>null</c>.</exception>
    public static string ToHexString(this byte[] bytes, bool upperCase = false, string? delimiter = null)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length > 0)
        {
            delimiter ??= string.Empty;

            int length = bytes.Length * 2 + (bytes.Length - 1) * delimiter.Length;

            StringBuilder sb = new(length);

            string format = upperCase ? "X2" : "x2";

            if (delimiter.Length == 0)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString(format));
                }
            }
            else
            {
                sb.Append(bytes[0].ToString(format));

                for (int i = 1; i < bytes.Length; i++)
                {
                    sb.Append(delimiter);
                    sb.Append(bytes[i].ToString(format));
                }
            }

            return sb.ToString();
        }

        return string.Empty;
    }
}
