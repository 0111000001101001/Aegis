using Microsoft.Data.Sqlite;

namespace Aegis.Services;

/// <summary>
/// Manages user-related operations such as creation and authentication.
/// </summary>
public class UserService : IUserService
{
    /// <summary>
    /// The reserved username for the master database that cannot be used.
    /// </summary>
    private const string RESERVED_MASTER_DATABASE_NAME = "aegis";

    /// <summary>
    /// Minimum allowed username length.
    /// </summary>
    private const int MIN_USERNAME_LENGTH = 5;

    /// <summary>
    /// Maximum allowed username length.
    /// </summary>
    private const int MAX_USERNAME_LENGTH = 16;

    /// <summary>
    /// Minimum allowed password length.
    /// </summary>
    private const int MIN_PASSWORD_LENGTH = 8;

    /// <summary>
    /// Maximum allowed password length.
    /// </summary>
    private const int MAX_PASSWORD_LENGTH = 128;

    /// <summary>
    /// Service for database operations.
    /// </summary>
    private readonly IDatabaseService _dbService;

    /// <summary>
    /// Service for cryptographic operations.
    /// </summary>
    private readonly ICryptoService _cryptoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </summary>
    /// <param name="dbService">The database service instance.</param>
    /// <param name="cryptoService">The cryptography service instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public UserService(IDatabaseService dbService, ICryptoService cryptoService)
    {
        ArgumentNullException.ThrowIfNull(dbService, nameof(dbService));
        ArgumentNullException.ThrowIfNull(cryptoService, nameof(cryptoService));

        _dbService = dbService;
        _cryptoService = cryptoService;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Thrown when the username or password do not meet requirements.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the username is already taken.</exception>
    public User CreateUser(string username, string password)
    {
        ValidateUsername(username);
        ValidatePassword(password);

        // Check if the username already exists in the database
        long existingUser = _dbService.ExecuteScalar<long>(
            "SELECT COUNT(1) FROM Users WHERE Username = @user",
            new SqliteParameter("@user", username)
        );

        if (existingUser > 0)
        {
            throw new InvalidOperationException("This username is already taken.");
        }

        // Hash the master password with a new salt
        string hash = _cryptoService.HashMasterPassword(password, out byte[] salt);

        // Insert the new user into the database
        _dbService.ExecuteNonQuery(
            "INSERT INTO Users (Username, MasterPasswordHash, Salt) VALUES (@user, @hash, @salt)",
            new SqliteParameter("@user", username),
            new SqliteParameter("@hash", hash),
            new SqliteParameter("@salt", Convert.ToBase64String(salt))
        );

        // Authenticate the new user to create a session
        return AuthenticateUser(username, password)!;
    }

    /// <inheritdoc/>
    public User? AuthenticateUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        // Retrieve user data from the database
        Dictionary<string, object>? result = _dbService
            .ExecuteQuery(
                "SELECT Id, Username, MasterPasswordHash, Salt FROM Users WHERE Username = @user",
                new SqliteParameter("@user", username)
            )
            .FirstOrDefault();

        if (result == null)
        {
            return null;
        }

        // Extract hash and salt from the database record
        string storedHash = (string)result["MasterPasswordHash"];
        byte[] salt = Convert.FromBase64String((string)result["Salt"]);

        // Verify the provided password against the stored hash
        if (_cryptoService.VerifyMasterPassword(password, storedHash, salt))
        {
            // If successful, return a User object
            return new User
            {
                Id = (long)result["Id"],
                Username = (string)result["Username"],
                MasterPassword = password, // Hold password in memory for the session to derive encryption key
                Salt = salt,
            };
        }

        return null;
    }

    /// <summary>
    /// Validates the username format and length.
    /// </summary>
    /// <param name="username">The username to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the username does not meet requirements.</exception>
    private static void ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                $"Username must be {MIN_USERNAME_LENGTH}-{MAX_USERNAME_LENGTH} characters long and contain only letters and numbers.",
                nameof(username)
            );
        }

        if (username.Length < MIN_USERNAME_LENGTH || username.Length > MAX_USERNAME_LENGTH)
        {
            throw new ArgumentException(
                $"Username must be {MIN_USERNAME_LENGTH}-{MAX_USERNAME_LENGTH} characters long and contain only letters and numbers.",
                nameof(username)
            );
        }

        if (!username.All(char.IsLetterOrDigit))
        {
            throw new ArgumentException(
                $"Username must be {MIN_USERNAME_LENGTH}-{MAX_USERNAME_LENGTH} characters long and contain only letters and numbers.",
                nameof(username)
            );
        }

        // Check if the inputted username is equal to the name of the master database
        if (
            string.Equals(
                username,
                RESERVED_MASTER_DATABASE_NAME,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new ArgumentException(
                "This username is reserved for the master database.",
                nameof(username)
            );
        }
    }

    /// <summary>
    /// Validates the password length.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the password does not meet requirements.</exception>
    private static void ValidatePassword(string password)
    {
        if (
            string.IsNullOrWhiteSpace(password)
            || password.Length < MIN_PASSWORD_LENGTH
            || password.Length > MAX_PASSWORD_LENGTH
        )
        {
            throw new ArgumentException(
                $"Master password needs to be at least {MIN_PASSWORD_LENGTH} characters long with a limit of {MAX_PASSWORD_LENGTH} characters.",
                nameof(password)
            );
        }
    }
}
