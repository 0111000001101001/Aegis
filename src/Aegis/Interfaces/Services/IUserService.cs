namespace Aegis.Interfaces.Services;

/// <summary>
/// Defines the contract for user-related operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="username">The desired username.</param>
    /// <param name="password">The desired master password.</param>
    /// <returns>The created <see cref="User"/> object.</returns>
    User CreateUser(string username, string password);

    /// <summary>
    /// Authenticates a user based on username and password.
    /// </summary>
    /// <param name="username">The user's username.</param>
    /// <param name="password">The user's master password.</param>
    /// <returns>A <see cref="User"/> object if authentication is successful; otherwise, null.</returns>
    User? AuthenticateUser(string username, string password);
}
