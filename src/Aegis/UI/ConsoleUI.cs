using Spectre.Console;

namespace Aegis.UI;

/// <summary>
/// Handles the user interface for the console application.
/// </summary>
public static class ConsoleUI
{
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
    /// Displays the application banner.
    /// </summary>
    public static void ShowBanner()
    {
        AnsiConsole.MarkupLine(
            """
            [Bold Blue]
                                           
              ▄▄▄▄                         
            ▄██▀▀██▄             ▀▀        
            ███  ███ ▄█▀█▄ ▄████ ██  ▄█▀▀▀ 
            ███▀▀███ ██▄█▀ ██ ██ ██  ▀███▄ 
            ███  ███ ▀█▄▄▄ ▀████ ██▄ ▄▄▄█▀ 
                              ██           
                            ▀▀▀            
            [/]
            """
        );
    }

    /// <summary>
    /// Shows the account menu and returns the user's choice.
    /// </summary>
    /// <returns>The user's selected menu option.</returns>
    public static string ShowAccountMenu()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[Bold White]Account Menu[/]")
                .PageSize(3)
                .AddChoices("Log-in to an existing account", "Create a new account", "Quit program")
        );
    }

    /// <summary>
    /// Prompts the user for new account credentials.
    /// </summary>
    /// <returns>A tuple containing the username and password.</returns>
    public static (string Username, string Password) GetNewCredentials()
    {
        AnsiConsole.MarkupLine(
            $"\n[Bold White]Create Account:[/] "
                + $"\nUsernames can only contain letters and numbers with a minimum "
                + $"length of {MIN_USERNAME_LENGTH} characters and a maximum length of {MAX_USERNAME_LENGTH}."
        );

        string username = AnsiConsole.Ask<string>("[Cyan3]Username:[/]");
        string password = AnsiConsole.Prompt(
            new TextPrompt<string>(
                $"\nMaster password needs to be at least {MIN_PASSWORD_LENGTH} characters long "
                    + $"with a limit of {MAX_PASSWORD_LENGTH} characters.\n[Cyan3]Master password:[/] "
            ).Secret()
        );

        return (username, password);
    }

    /// <summary>
    /// Prompts the user for login credentials.
    /// </summary>
    /// <returns>A tuple containing the username and password.</returns>
    public static (string Username, string Password) GetLoginCredentials()
    {
        AnsiConsole.MarkupLine("\n[Bold White]Log-in:[/]");
        string username = AnsiConsole.Ask<string>("[Cyan3]Username:[/]");
        string password = AnsiConsole.Prompt(
            new TextPrompt<string>("[Cyan3]Master password:[/] ").Secret()
        );

        return (username, password);
    }

    /// <summary>
    /// Shows the main menu and returns the user's choice.
    /// </summary>
    /// <returns>The user's selected menu option.</returns>
    public static string ShowMainMenu()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n\n[White]Select from the following commands:[/]")
                .PageSize(10)
                .AddChoices(
                    "Add new password",
                    "View full list of entries",
                    "Update an existing password",
                    "Delete an existing password",
                    "Search for an existing password",
                    "Generate a random password",
                    "Quit program"
                )
        );
    }

    /// <summary>
    /// Displays a list of credentials in a table format.
    /// </summary>
    /// <param name="credentials">The list of credentials to display.</param>
    /// <exception cref="ArgumentNullException">Thrown when credentials is null.</exception>
    public static void DisplayCredentials(IReadOnlyList<CredentialEntry> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials, nameof(credentials));

        Table table = new Table().Expand();
        table.Title("[Bold Blue]Your Credentials[/]");
        table.AddColumn("ID");
        table.AddColumn("Platform");
        table.AddColumn("Username");
        table.AddColumn("Password");

        foreach (CredentialEntry credential in credentials)
        {
            // Escape special characters
            table.AddRow(
                credential.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Markup.Escape(credential.Platform),
                Markup.Escape(credential.Username),
                Markup.Escape(credential.Password)
            );
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Pauses execution and waits for the user to press a key to return to the main menu.
    /// </summary>
    public static void ReturnToMenu()
    {
        AnsiConsole.Markup("\nPress [Blue]any key[/] to return to the menu...");
        Console.ReadKey(intercept: true);
    }
}
