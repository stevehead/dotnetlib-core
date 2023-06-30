using System;
using System.IO;
using System.Security.Cryptography;

namespace Stevehead.IO;

/// <summary>
/// FileInfo extension methods.
/// </summary>
public static class FileInfoExtensions
{
    /// <summary>
    /// Computes the MD5 hash of the provided file.
    /// </summary>
    /// 
    /// <param name="file">The file to be hashed.</param>
    /// <returns>A string representing the hex value of the computed hash.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="file"/> is <c>null</c>.</exception>
    public static string ComputeMD5(this FileInfo file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        byte[] bytes;

        using (var stream = file.OpenRead())
        {
            bytes = MD5.HashData(stream);
        }

        return bytes.ToHexString(upperCase: false);
    }
}
