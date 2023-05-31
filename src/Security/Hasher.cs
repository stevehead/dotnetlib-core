using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Stevehead.Security;

/// <summary>
/// Utility methods for hashing text.
/// </summary>
public static class Hasher
{
    /// <summary>
    /// The number of characters in a computed hash string.
    /// </summary>
    public const int HashLength = 64;

    /// <summary>
    /// The number of characters in a computed salt string.
    /// </summary>
    public const int SaltLength = 64;

    /// <summary>
    /// Generates a new salt value, then hashes the provided text using the generated salt value.
    /// </summary>
    /// 
    /// <param name="text">The text to be hashed.</param>
    /// <param name="hash">The computed hash value.</param>
    /// <param name="salt">The salt value randomly generated and used to compute <paramref name="hash"/>.</param>
    public static void Hash(string? text, out string hash, out string salt)
    {
        byte[] saltBytes1 = Guid.NewGuid().ToByteArray();
        byte[] saltBytes2 = Guid.NewGuid().ToByteArray();
        salt = ToHexString(saltBytes1) + ToHexString(saltBytes2);
        hash = Hash(text, salt);
    }

    /// <summary>
    /// Hashes the provided text value using the provided salt value.
    /// </summary>
    /// 
    /// <param name="text">The text to be hashed.</param>
    /// <param name="salt">The salt to be used.</param>
    /// <returns>The computed hash value.</returns>
    public static string Hash(string? text, string? salt)
    {
        text ??= string.Empty;
        salt ??= string.Empty;

        byte[] bytes;

        using (HashAlgorithm algorithm = SHA256.Create())
        {
            bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(text + salt));
        }

        return ToHexString(bytes);
    }

    private static string ToHexString(byte[] bytes)
    {
        Debug.Assert(bytes != null);

        StringBuilder sb = new(bytes.Length * 2);

        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }
}
