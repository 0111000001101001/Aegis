using Microsoft.Data.Sqlite;

namespace Aegis.Interfaces.Services;

/// <summary>
/// Defines the contract for database operations.
/// </summary>
public interface IDatabaseService : IDisposable
{
    /// <summary>
    /// Initializes the master database with the Users table.
    /// </summary>
    void InitializeMasterDatabase();

    /// <summary>
    /// Initializes a user's vault database with the Credentials table.
    /// </summary>
    void InitializeVaultDatabase();

    /// <summary>
    /// Executes a non-query SQL command (e.g., INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Parameters to use in the SQL query.</param>
    void ExecuteNonQuery(string query, params SqliteParameter[] parameters);

    /// <summary>
    /// Executes a query that returns a single value.
    /// </summary>
    /// <typeparam name="T">The type of the value to return.</typeparam>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Parameters to use in the SQL query.</param>
    /// <returns>The single value result of the query, converted to type T.</returns>
    T ExecuteScalar<T>(string query, params SqliteParameter[] parameters);

    /// <summary>
    /// Executes a query and returns the results as a list of dictionaries.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Parameters to use in the SQL query.</param>
    /// <returns>A list of dictionaries, where each dictionary represents a row with column names as keys.</returns>
    List<Dictionary<string, object>> ExecuteQuery(
        string query,
        params SqliteParameter[] parameters
    );
}
