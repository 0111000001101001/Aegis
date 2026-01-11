using System.Security.Cryptography;
using System.Text;

namespace Aegis.Services;

/// <summary>
/// Provides cryptographic services for hashing, encryption, and decryption.
/// </summary>
public class CryptoService : ICryptoService
{
    /// <summary>
    /// Defines the size of the salt for hashing in bytes. 16 * 8 = 128 bits.
    /// </summary>
    private const int SALT_SIZE = 16;

    /// <summary>
    /// Defines the size of the key for encryption in bytes. 32 * 8 = 256 bits.
    /// </summary>
    private const int KEY_SIZE = 32;

    /// <summary>
    /// Defines the number of iterations for the PBKDF2 function. Higher numbers increase security but also processing time.
    /// </summary>
    private const int ITERATIONS = 100000;

    /// <summary>
    /// Specifies the hash algorithm to be used with PBKDF2. SHA256 is a secure choice.
    /// </summary>
    private static readonly HashAlgorithmName s_hashAlgorithm = HashAlgorithmName.SHA256;

    /// <inheritdoc/>
    public string HashMasterPassword(string password, out byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));

        salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            ITERATIONS,
            s_hashAlgorithm,
            KEY_SIZE
        );

        return Convert.ToBase64String(hash);
    }

    /// <inheritdoc/>
    public bool VerifyMasterPassword(string password, string hash, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentException.ThrowIfNullOrEmpty(hash, nameof(hash));
        ArgumentNullException.ThrowIfNull(salt, nameof(salt));

        byte[] hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            ITERATIONS,
            s_hashAlgorithm,
            KEY_SIZE
        );

        // Compares the computed hash with the stored hash in a way that avoids timing attacks.
        return CryptographicOperations.FixedTimeEquals(
            hashToCompare,
            Convert.FromBase64String(hash)
        );
    }

    /// <inheritdoc/>
    public byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrEmpty(password, nameof(password));
        ArgumentNullException.ThrowIfNull(salt, nameof(salt));

        // Use PBKDF2 to derive a secure key from the master password
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt, // Use the same salt as for hashing, or a different one. For simplicity, we reuse.
            ITERATIONS,
            s_hashAlgorithm,
            KEY_SIZE
        );
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(plainText, nameof(plainText));
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Generate a new IV for each encryption to ensure security.

        // Create an encryptor to perform the stream transform.
        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using MemoryStream msEncrypt = new();
        using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
        using (StreamWriter swEncrypt = new(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }

        byte[] encryptedContent = msEncrypt.ToArray();

        // Prepend the IV to the encrypted content. This is crucial for decryption.
        byte[] result = new byte[aes.IV.Length + encryptedContent.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedContent, 0, result, aes.IV.Length, encryptedContent.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc/>
    public string Decrypt(string cipherText, byte[] key)
    {
        ArgumentException.ThrowIfNullOrEmpty(cipherText, nameof(cipherText));
        ArgumentNullException.ThrowIfNull(key, nameof(key));

        byte[] fullCipher = Convert.FromBase64String(cipherText);
        using Aes aes = Aes.Create();

        // Extract the IV from the beginning of the cipher text.
        int ivSize = aes.BlockSize / 8;
        byte[] iv = new byte[ivSize];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, ivSize);

        // Extract the actual encrypted content.
        byte[] cipher = new byte[fullCipher.Length - ivSize];
        Buffer.BlockCopy(fullCipher, ivSize, cipher, 0, cipher.Length);

        aes.Key = key;
        aes.IV = iv;

        // Create a decryptor to perform the stream transform.
        using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using MemoryStream msDecrypt = new(cipher);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new(csDecrypt);

        // Read the decrypted bytes from the decrypting stream and place them in a string.
        return srDecrypt.ReadToEnd();
    }
}
