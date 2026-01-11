using Microsoft.Data.Sqlite;

namespace Aegis.Services;

/// <summary>
/// Provides services for interacting with the SQLite database.
/// </summary>
public class DatabaseService : IDatabaseService
{
    /// <summary>
    /// The connection string for the SQLite database.
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseService"/> class.
    /// </summary>
    /// <param name="dbPath">The path to the SQLite database file.</param>
    /// <exception cref="ArgumentException">Thrown when the database path is null or empty.</exception>
    public DatabaseService(string dbPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dbPath, nameof(dbPath));
        _connectionString = $"Data Source={dbPath}";
    }

    /// <inheritdoc/>
    /// <exception cref="SqliteException">Thrown when there is an error executing the SQL command.</exception>
    public void InitializeMasterDatabase()
    {
        const string CREATE_USERS_TABLE_SQL = """
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                MasterPasswordHash TEXT NOT NULL,
                Salt TEXT NOT NULL
            );
            """;

        try
        {
            using SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = CREATE_USERS_TABLE_SQL;
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Error initializing master database: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="SqliteException">Thrown when there is an error executing the SQL command.</exception>
    public void InitializeVaultDatabase()
    {
        const string CREATE_CREDENTIALS_TABLE_SQL = """
            CREATE TABLE IF NOT EXISTS Credentials (
                Id INTEGER PRIMARY KEY,
                EncryptedPlatform TEXT NOT NULL,
                Username TEXT NOT NULL,
                EncryptedPassword TEXT NOT NULL
            );
            """;

        try
        {
            using SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = CREATE_CREDENTIALS_TABLE_SQL;
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Error initializing vault database: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="SqliteException">Thrown when there is an error executing the SQL command.</exception>
    public void ExecuteNonQuery(string query, params SqliteParameter[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        try
        {
            using SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddRange(parameters);
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Error executing non-query: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="SqliteException">Thrown when there is an error executing the SQL command.</exception>
    /// <exception cref="InvalidCastException">Thrown when there is an error converting the result to type T.</exception>
    public T ExecuteScalar<T>(string query, params SqliteParameter[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        try
        {
            using SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddRange(parameters);
            object? result = command.ExecuteScalar();

            if (result is null or DBNull)
            {
                // Return default value for value types, or null for reference types
                return default!;
            }

            return (T)
                Convert.ChangeType(
                    result,
                    typeof(T),
                    System.Globalization.CultureInfo.InvariantCulture
                );
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Error executing scalar query: {ex.Message}");
            throw;
        }
        catch (InvalidCastException ex)
        {
            Console.WriteLine($"Error converting scalar result: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="SqliteException">Thrown when there is an error executing the SQL command.</exception>
    public List<Dictionary<string, object>> ExecuteQuery(
        string query,
        params SqliteParameter[] parameters
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query, nameof(query));

        try
        {
            List<Dictionary<string, object>> results = [];
            using SqliteConnection connection = new(_connectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddRange(parameters);

            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                Dictionary<string, object> row = new(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }

                results.Add(row);
            }

            return results;
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"Error executing query: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Closes any pooled connections for this database.
    /// </summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.SuppressFinalize(this);
    }
}
