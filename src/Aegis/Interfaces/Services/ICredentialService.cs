namespace Aegis.Interfaces.Services;

/// <summary>
/// Defines the contract for credential management operations.
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Adds a new credential for a user.
    /// </summary>
    /// <param name="encryptionKey">The key used for encrypting the credential data.</param>
    void AddCredential(byte[] encryptionKey);

    /// <summary>
    /// Retrieves and displays all credentials for the current user.
    /// </summary>
    /// <param name="encryptionKey">The key for decrypting credential data.</param>
    void ViewAllCredentials(byte[] encryptionKey);

    /// <summary>
    /// Updates the password for an existing credential.
    /// </summary>
    /// <param name="encryptionKey">The key for encrypting the new password.</param>
    void UpdateCredential(byte[] encryptionKey);

    /// <summary>
    /// Deletes a credential entry.
    /// </summary>
    void DeleteCredential();

    /// <summary>
    /// Searches for credentials by platform name.
    /// </summary>
    /// <param name="encryptionKey">The key for decrypting credential data.</param>
    void SearchCredentials(byte[] encryptionKey);
}
