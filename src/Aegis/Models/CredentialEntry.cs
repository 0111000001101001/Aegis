namespace Aegis.Models;

/// <summary>
/// Represents a single credential entry, such as a password for a platform.
/// </summary>
public sealed class CredentialEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the credential entry.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Gets or sets the platform name associated with the credential.
    /// </summary>
    public required string Platform { get; init; }

    /// <summary>
    /// Gets or sets the username or email for the credential.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Gets or sets the password for the credential.
    /// </summary>
    public required string Password { get; init; }
}
