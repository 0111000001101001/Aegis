namespace Aegis.Models;

/// <summary>
/// Represents a user of the password manager.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the user's master password.
    /// This is only stored in memory for the duration of the user's session
    /// to derive the encryption key for credential operations.
    /// It is not stored in the database.
    /// </summary>
    public required string MasterPassword { get; init; } // Only stored in memory for the duration of the session

    /// <summary>
    /// Gets or sets the salt used for hashing the master password.
    /// </summary>
    public required byte[] Salt { get; init; }
}
