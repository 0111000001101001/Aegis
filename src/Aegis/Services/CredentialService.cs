using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Spectre.Console;

namespace Aegis.Services;

/// <summary>
/// Manages user credentials, including adding, viewing, and updating them.
/// </summary>
public class CredentialService : ICredentialService
{
    /// <summary>
    /// The default length for generated random passwords.
    /// </summary>
    private const int GENERATED_PASSWORD_LENGTH = 32;

    /// <summary>
    /// Character set used for random password generation.
    /// </summary>
    private const string PASSWORD_CHARACTER_SET =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@&$?!#*^+-.";

    /// <summary>
    /// Service for database operations.
    /// </summary>
    private readonly IDatabaseService _dbService;

    /// <summary>
    /// Service for cryptographic operations.
    /// </summary>
    private readonly ICryptoService _cryptoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialService"/> class.
    /// </summary>
    /// <param name="dbService">The database service instance.</param>
    /// <param name="cryptoService">The cryptography service instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public CredentialService(IDatabaseService dbService, ICryptoService cryptoService)
    {
        ArgumentNullException.ThrowIfNull(dbService, nameof(dbService));
        ArgumentNullException.ThrowIfNull(cryptoService, nameof(cryptoService));

        _dbService = dbService;
        _cryptoService = cryptoService;
    }

    /// <inheritdoc/>
    public void AddCredential(byte[] encryptionKey)
    {
        // Prompt user for credential details
        string platform = AnsiConsole.Ask<string>("\nPlatform name:");
        string username = AnsiConsole.Ask<string>("Username or email:");
        string password = AnsiConsole.Prompt(new TextPrompt<string>("Password:").Secret());

        // Encrypt platform name, username, and password
        string encryptedPlatform = _cryptoService.Encrypt(platform, encryptionKey);
        string encryptedUsername = _cryptoService.Encrypt(username, encryptionKey);
        string encryptedPassword = _cryptoService.Encrypt(password, encryptionKey);

        // Get the next available ID
        long newId = GetNextAvailableId();

        // Insert the new credential into the database
        _dbService.ExecuteNonQuery(
            "INSERT INTO Credentials (Id, EncryptedPlatform, Username, EncryptedPassword) VALUES (@id, @plat, @user, @pass)",
            new SqliteParameter("@id", newId),
            new SqliteParameter("@plat", encryptedPlatform),
            new SqliteParameter("@user", encryptedUsername),
            new SqliteParameter("@pass", encryptedPassword)
        );

        AnsiConsole.MarkupLine("\n[Magenta]Password successfully added. ＼ʕ •ᴥ•ʔ／[/]");
    }

    /// <inheritdoc/>
    public void ViewAllCredentials(byte[] encryptionKey)
    {
        // Retrieve all credentials for the user from the database
        List<Dictionary<string, object>> results = _dbService.ExecuteQuery(
            "SELECT Id, EncryptedPlatform, Username, EncryptedPassword FROM Credentials ORDER BY Id"
        );

        // Check if any credentials were found
        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[Red]No entries found. ʕ ´•̥̥̥ ᴥ•̥̥̥`ʔ[/]");
            return;
        }

        // Decrypt and prepare credential data for display
        List<CredentialEntry> credentialList = DecryptCredentialList(results, encryptionKey);

        // Display the credentials in a table format
        ConsoleUI.DisplayCredentials(credentialList);
    }

    /// <inheritdoc/>
    public void UpdateCredential(byte[] encryptionKey)
    {
        // Prompt for the ID of the entry to update and the new password
        int entryId = AnsiConsole.Ask<int>("\nEnter the entry ID to update:");

        if (!CredentialExists(entryId))
        {
            AnsiConsole.MarkupLine("\n[Bold Red]Entry ID not found. ʕ ´•̥̥̥ ᴥ•̥̥̥`ʔ[/]");
            return;
        }

        string newPassword = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the new password:").Secret()
        );

        if (
            !AnsiConsole.Confirm(
                $"Are you sure you want to update the password for entry ID {entryId}?"
            )
        )
        {
            AnsiConsole.MarkupLine("[Red]Update cancelled.[/]");
            return;
        }

        // Encrypt the new password
        string encryptedPassword = _cryptoService.Encrypt(newPassword, encryptionKey);

        // Update the password in the database
        _dbService.ExecuteNonQuery(
            "UPDATE Credentials SET EncryptedPassword = @pass WHERE Id = @id",
            new SqliteParameter("@pass", encryptedPassword),
            new SqliteParameter("@id", entryId)
        );

        AnsiConsole.MarkupLine("\n[Magenta]Password successfully updated. ＼ʕ •ᴥ•ʔ／[/]");
    }

    /// <inheritdoc/>
    public void DeleteCredential()
    {
        int entryId = AnsiConsole.Ask<int>("\nEnter the entry ID to delete:");

        if (!CredentialExists(entryId))
        {
            AnsiConsole.MarkupLine("\n[Bold Red]Entry ID not found. ʕ ´•̥̥̥ ᴥ•̥̥̥`ʔ[/]");
            return;
        }

        if (
            !AnsiConsole.Confirm(
                $"[Bold Red]Are you sure you want to permanently delete entry ID {entryId}?[/]"
            )
        )
        {
            AnsiConsole.MarkupLine("[Red]Deletion cancelled.[/]");
            return;
        }

        _dbService.ExecuteNonQuery(
            "DELETE FROM Credentials WHERE Id = @id",
            new SqliteParameter("@id", entryId)
        );

        AnsiConsole.MarkupLine("\n[Magenta]Password successfully deleted. ＼ʕ •ᴥ•ʔ／[/]");
    }

    /// <inheritdoc/>
    public void SearchCredentials(byte[] encryptionKey)
    {
        string platformQuery = AnsiConsole.Ask<string>("\nEnter the platform name to search for:");

        // Retrieve all credentials and decrypt in memory to perform the search
        List<Dictionary<string, object>> allCredentials = _dbService.ExecuteQuery(
            "SELECT Id, EncryptedPlatform, Username, EncryptedPassword FROM Credentials"
        );

        List<CredentialEntry> matchingCredentials = [];
        foreach (Dictionary<string, object> row in allCredentials)
        {
            string decryptedPlatform = _cryptoService.Decrypt(
                (string)row["EncryptedPlatform"],
                encryptionKey
            );

            if (decryptedPlatform.Contains(platformQuery, StringComparison.OrdinalIgnoreCase))
            {
                matchingCredentials.Add(
                    new CredentialEntry
                    {
                        Id = (long)row["Id"],
                        Platform = decryptedPlatform,
                        Username = _cryptoService.Decrypt((string)row["Username"], encryptionKey),
                        Password = _cryptoService.Decrypt(
                            (string)row["EncryptedPassword"],
                            encryptionKey
                        ),
                    }
                );
            }
        }

        if (matchingCredentials.Count == 0)
        {
            AnsiConsole.MarkupLine("[Red]No matching entries found. ʕ ´•̥̥̥ ᴥ•̥̥̥`ʔ[/]");
            return;
        }

        ConsoleUI.DisplayCredentials(matchingCredentials);
    }

    /// <summary>
    /// Generates a cryptographically secure random password.
    /// </summary>
    public static void GenerateRandomPassword()
    {
        StringBuilder password = new(GENERATED_PASSWORD_LENGTH);

        // Use cryptographically secure random number generator
        for (int i = 0; i < GENERATED_PASSWORD_LENGTH; i++)
        {
            int index = RandomNumberGenerator.GetInt32(PASSWORD_CHARACTER_SET.Length);
            password.Append(PASSWORD_CHARACTER_SET[index]);
        }

        string generatedPassword = password.ToString();

        // Escape special characters to prevent markup issues
        AnsiConsole.MarkupLine(
            $"\nGenerated password: [Magenta]{Markup.Escape(generatedPassword)}[/]"
        );

        if (AnsiConsole.Confirm("\nWould you like to copy this password to clipboard?"))
        {
            CopyPasswordToClipboard(generatedPassword);
        }
    }

    /// <summary>
    /// Decrypts a list of credential rows from the database.
    /// </summary>
    /// <param name="results">The raw database results.</param>
    /// <param name="encryptionKey">The key for decrypting credential data.</param>
    /// <returns>A list of decrypted credential entries.</returns>
    private List<CredentialEntry> DecryptCredentialList(
        List<Dictionary<string, object>> results,
        byte[] encryptionKey
    )
    {
        List<CredentialEntry> credentialList = new(results.Count);

        foreach (Dictionary<string, object> row in results)
        {
            credentialList.Add(
                new CredentialEntry
                {
                    Id = (long)row["Id"],
                    Platform = _cryptoService.Decrypt(
                        (string)row["EncryptedPlatform"],
                        encryptionKey
                    ),
                    Username = _cryptoService.Decrypt((string)row["Username"], encryptionKey),
                    Password = _cryptoService.Decrypt(
                        (string)row["EncryptedPassword"],
                        encryptionKey
                    ),
                }
            );
        }

        return credentialList;
    }

    /// <summary>
    /// Copies a password to the system clipboard.
    /// </summary>
    /// <param name="password">The password to copy.</param>
    private static void CopyPasswordToClipboard(string password)
    {
        try
        {
            TextCopy.ClipboardService.SetText(password);
            AnsiConsole.MarkupLine("\n[Magenta]Password copied to clipboard! ＼ʕ •ᴥ•ʔ／[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"\n[Bold Red]Failed to copy to clipboard: {ex.Message}[/]");
        }
    }

    /// <summary>
    /// Checks if a credential with the specified ID exists.
    /// </summary>
    /// <param name="entryId">The ID to check.</param>
    /// <returns>True if the credential exists; otherwise, false.</returns>
    private bool CredentialExists(long entryId)
    {
        long count = _dbService.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Credentials WHERE Id = @id",
            new SqliteParameter("@id", entryId)
        );
        return count > 0;
    }

    /// <summary>
    /// Finds the lowest available ID for a new credential entry.
    /// </summary>
    /// <returns>The lowest available ID.</returns>
    private long GetNextAvailableId()
    {
        List<Dictionary<string, object>> results = _dbService.ExecuteQuery(
            "SELECT Id FROM Credentials ORDER BY Id"
        );
        HashSet<long> ids = [.. results.Select(row => (long)row["Id"])];

        long nextId = 1;
        while (ids.Contains(nextId))
        {
            nextId++;
        }

        return nextId;
    }
}
