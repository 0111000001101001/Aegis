namespace Aegis.Interfaces.Services;

/// <summary>
/// Defines the contract for cryptographic operations.
/// </summary>
public interface ICryptoService
{
    /// <summary>
    /// Hashes a master password using PBKDF2 with a randomly generated salt.
    /// </summary>
    /// <param name="password">The master password to hash.</param>
    /// <param name="salt">An out parameter that will contain the generated salt.</param>
    /// <returns>A Base64 encoded string representing the hashed password.</returns>
    string HashMasterPassword(string password, out byte[] salt);

    /// <summary>
    /// Verifies a master password against a stored hash and salt.
    /// </summary>
    /// <param name="password">The master password to verify.</param>
    /// <param name="hash">The stored hash as a Base64 string.</param>
    /// <param name="salt">The salt used when the original hash was created.</param>
    /// <returns>True if the password is correct; otherwise, false.</returns>
    bool VerifyMasterPassword(string password, string hash, byte[] salt);

    /// <summary>
    /// Derives a cryptographic key from a password and salt using PBKDF2.
    /// </summary>
    /// <param name="password">The password to derive the key from.</param>
    /// <param name="salt">The salt to use in the derivation.</param>
    /// <returns>A byte array representing the derived key.</returns>
    byte[] DeriveKeyFromPassword(string password, byte[] salt);

    /// <summary>
    /// Encrypts a plaintext string using AES with a given key.
    /// </summary>
    /// <param name="plainText">The string to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>A Base64 encoded string containing the IV and the encrypted ciphertext.</returns>
    string Encrypt(string plainText, byte[] key);

    /// <summary>
    /// Decrypts a ciphertext string using AES with a given key.
    /// </summary>
    /// <param name="cipherText">The Base64 encoded string containing the IV and ciphertext.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The original decrypted string.</returns>
    string Decrypt(string cipherText, byte[] key);
}
