using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Stevehead.Security;

/// <summary>
/// Utility methods for encrypting/decrypting data.
/// </summary>
public static class Encryption
{
    private static readonly byte[] DefaultIV =
    {
        0x6F, 0xA7, 0xFC, 0xAF, 0x4A, 0x11, 0xB0, 0x72,
        0x66, 0xBB, 0x5E, 0x4B, 0x65, 0xE4, 0xA4, 0x4A,
    };

    private static readonly byte[] DefaultSalt =
    {
        0x77, 0xA8, 0x69, 0x22, 0xFF, 0x9D, 0xF1, 0x6A,
        0xEB, 0x97, 0xD5, 0x64, 0x5F, 0x75, 0x42, 0x09,
    };

    /// <summary>
    /// Encrypts the provided data using the provided pass key and optional IV and salt values.
    /// </summary>
    /// 
    /// <param name="dataToEncrypt">The data to be encrypted.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// <returns>A byte array that is the encryption result of <paramref name="dataToEncrypt"/>.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="passkey"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> dataToEncrypt, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);

        cryptoStream.Write(dataToEncrypt);
        cryptoStream.FlushFinalBlock();

        return output.ToArray();
    }

    /// <summary>
    /// Encrypts data from the provided input stream using the provided pass key and optional IV and salt values, and
    /// writes the encrypted data to a provided output stream.
    /// </summary>
    /// 
    /// <param name="inputStream">The stream that provides the data to be encrypted.</param>
    /// <param name="outputStream">The stream that is the target of the encrypted data.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// 
    /// <exception cref="ArgumentNullException">
    ///          <paramref name="inputStream"/> is <c>null</c>.
    ///     -or- <paramref name="outputStream"/> is <c>null</c>.
    ///     -or- <paramref name="passkey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static void Encrypt(Stream inputStream, Stream outputStream, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (inputStream == null)
        {
            throw new ArgumentNullException(nameof(inputStream));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        inputStream.CopyTo(cryptoStream);
        cryptoStream.FlushFinalBlock();
    }

    /// <summary>
    /// Asynchronously encrypts the provided data using the provided pass key and optional IV and salt values.
    /// </summary>
    /// 
    /// <param name="dataToEncrypt">The data to be encrypted.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// <returns>A byte array that is the encryption result of <paramref name="dataToEncrypt"/>.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="passkey"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static async Task<byte[]> EncryptAsync(byte[] dataToEncrypt, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (dataToEncrypt == null)
        {
            throw new ArgumentNullException(nameof(dataToEncrypt));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(dataToEncrypt);
        await cryptoStream.FlushFinalBlockAsync();

        return output.ToArray();
    }

    /// <summary>
    /// Asynchronously encrypts data from the provided input stream using the provided pass key and optional IV and salt
    /// values, and writes the encrypted data to a provided output stream.
    /// </summary>
    /// 
    /// <param name="inputStream">The stream that provides the data to be encrypted.</param>
    /// <param name="outputStream">The stream that is the target of the encrypted data.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// 
    /// <exception cref="ArgumentNullException">
    ///          <paramref name="inputStream"/> is <c>null</c>.
    ///     -or- <paramref name="outputStream"/> is <c>null</c>.
    ///     -or- <paramref name="passkey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static async Task EncryptAsync(Stream inputStream, Stream outputStream, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (inputStream == null)
        {
            throw new ArgumentNullException(nameof(inputStream));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await inputStream.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();
    }

    /// <summary>
    /// Decrypts the provided data using the provided pass key and optional IV and salt values.
    /// </summary>
    /// 
    /// <param name="encryptedData">The data to be decrypted.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// <returns>A byte array that is the decryption result of <paramref name="encryptedData"/>.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="passkey"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static byte[] Decrypt(byte[] encryptedData, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (encryptedData == null)
        {
            throw new ArgumentNullException(nameof(encryptedData));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using MemoryStream input = new(encryptedData);
        using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);

        using MemoryStream output = new();
        cryptoStream.CopyTo(output);

        return output.ToArray();
    }

    /// <summary>
    /// Decrypts data from the provided input stream using the provided pass key and optional IV and salt values, and
    /// writes the decrypted data to a provided output stream.
    /// </summary>
    /// 
    /// <param name="inputStream">The stream that provides the data to be decrypted.</param>
    /// <param name="outputStream">The stream that is the target of the decrypted data.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// 
    /// <exception cref="ArgumentNullException">
    ///          <paramref name="inputStream"/> is <c>null</c>.
    ///     -or- <paramref name="outputStream"/> is <c>null</c>.
    ///     -or- <paramref name="passkey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static void Decrypt(Stream inputStream, Stream outputStream, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (inputStream == null)
        {
            throw new ArgumentNullException(nameof(inputStream));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using CryptoStream cryptoStream = new(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        cryptoStream.CopyTo(outputStream);
    }

    /// <summary>
    /// Asynchronously decrypts the provided data using the provided pass key and optional IV and salt values.
    /// </summary>
    /// 
    /// <param name="encryptedData">The data to be decrypted.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// <returns>A byte array that is the decryption result of <paramref name="encryptedData"/>.</returns>
    /// 
    /// <exception cref="ArgumentNullException"><paramref name="passkey"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static async Task<byte[]> DecryptAsync(byte[] encryptedData, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (encryptedData == null)
        {
            throw new ArgumentNullException(nameof(encryptedData));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using MemoryStream input = new(encryptedData);
        using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);

        using MemoryStream output = new();
        await cryptoStream.CopyToAsync(output);

        return output.ToArray();
    }

    /// <summary>
    /// Asynchronously decrypts data from the provided input stream using the provided pass key and optional IV and salt
    /// values, and writes the decrypted data to a provided output stream.
    /// </summary>
    /// 
    /// <param name="inputStream">The stream that provides the data to be decrypted.</param>
    /// <param name="outputStream">The stream that is the target of the decrypted data.</param>
    /// <param name="passkey">The pass key.</param>
    /// <param name="iv">The optional IV.</param>
    /// <param name="salt">The optional salt.</param>
    /// 
    /// <exception cref="ArgumentNullException">
    ///          <paramref name="inputStream"/> is <c>null</c>.
    ///     -or- <paramref name="outputStream"/> is <c>null</c>.
    ///     -or- <paramref name="passkey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="iv"/> is not <c>null</c> and is not 16 bytes in length.</exception>
    public static async Task DecryptAsync(Stream inputStream, Stream outputStream, string passkey, byte[]? iv = null, byte[]? salt = null)
    {
        if (inputStream == null)
        {
            throw new ArgumentNullException(nameof(inputStream));
        }

        if (outputStream == null)
        {
            throw new ArgumentNullException(nameof(outputStream));
        }

        if (passkey == null)
        {
            throw new ArgumentNullException(nameof(passkey));
        }

        if (iv != null && iv.Length != DefaultIV.Length)
        {
            throw new ArgumentException($"IV must be {DefaultIV.Length} bytes in length.");
        }

        using Aes aes = Aes.Create();
        aes.Key = DeriveKey(passkey, salt ?? DefaultSalt);
        aes.IV = iv ?? DefaultIV;

        using CryptoStream cryptoStream = new(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(outputStream);
    }

    // Internal method to derive the real key to use.
    private static byte[] DeriveKey(string passkey, byte[] salt)
    {
        byte[] password = Encoding.UTF8.GetBytes(passkey);
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 1000, HashAlgorithmName.SHA384, 16);
    }
}
