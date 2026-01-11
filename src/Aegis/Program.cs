using Spectre.Console;

namespace Aegis;

/// <summary>
/// The main entry point for Aegis.
/// </summary>
public static class Program
{
    /// <summary>
    /// The name of the directory where all database files are stored.
    /// </summary>
    private const string DATA_DIRECTORY_NAME = "data";

    /// <summary>
    /// The filename of the master database.
    /// </summary>
    private const string MASTER_DATABASE_FILE_NAME = "aegis.db";

    /// <summary>
    /// Maximum number of login attempts before the application exits.
    /// </summary>
    private const int MAX_LOGIN_ATTEMPTS = 5;

    /// <summary>
    /// The directory where all database files are stored.
    /// </summary>
    private static readonly string s_dataDirectory = Path.Combine(
        GetSolutionRoot(),
        DATA_DIRECTORY_NAME
    );

    /// <summary>
    /// The path for the master database file.
    /// </summary>
    private static readonly string s_masterDatabasePath = Path.Combine(
        s_dataDirectory,
        MASTER_DATABASE_FILE_NAME
    );

    /// <summary>
    /// List of database services to close on exit.
    /// </summary>
    private static readonly List<IDatabaseService> s_databaseServices = [];

    /// <summary>
    /// Service for user-related operations like creation and authentication.
    /// </summary>
    private static IUserService? s_userService;

    /// <summary>
    /// The currently logged-in user.
    /// </summary>
    private static User? s_currentUser;

    /// <summary>
    /// The main method that starts the application.
    /// </summary>
    public static void Main()
    {
        InitializeApplication();

        Console.Clear();
        ConsoleUI.ShowBanner();

        string? sessionPassword = null;

        // Loop until a user is successfully created or logged in
        while (s_currentUser == null)
        {
            string choice = ConsoleUI.ShowAccountMenu();
            sessionPassword = choice switch
            {
                "Log-in to an existing account" => HandleLogin(),
                "Create a new account" => HandleCreateAccount(),
                "Quit program" => HandleQuit(),
                _ => null,
            };
        }

        // If login/creation was successful, show the main menu
        if (s_currentUser != null && sessionPassword != null)
        {
            RunMainLoop(sessionPassword);
        }
    }

    /// <summary>
    /// Finds the solution root directory by searching upward for a marker file or folder.
    /// </summary>
    /// <returns>The path to the solution root directory.</returns>
    private static string GetSolutionRoot()
    {
        string? currentDirectory = AppContext.BaseDirectory;

        while (currentDirectory != null)
        {
            // Look for markers that indicate the solution root (the src directory)
            string srcPath = Path.Combine(currentDirectory, "src");
            string aegisPath = Path.Combine(currentDirectory, "src", "Aegis");

            if (Directory.Exists(srcPath) && Directory.Exists(aegisPath))
            {
                return currentDirectory;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        // Fallback to current directory if solution root is not found
        return Directory.GetCurrentDirectory();
    }

    /// <summary>
    /// Initializes the application by setting up directories and services.
    /// </summary>
    private static void InitializeApplication()
    {
        // Ensure the data directory exists
        Directory.CreateDirectory(s_dataDirectory);

        // Initialize services for the master database
        IDatabaseService masterDbService = new DatabaseService(s_masterDatabasePath);
        s_databaseServices.Add(masterDbService);
        masterDbService.InitializeMasterDatabase();

        ICryptoService cryptoService = new CryptoService();
        s_userService = new UserService(masterDbService, cryptoService);
    }

    /// <summary>
    /// Handles the login menu option.
    /// </summary>
    /// <returns>The user's password if login is successful; otherwise, null.</returns>
    private static string? HandleLogin()
    {
        return PerformLogin();
    }

    /// <summary>
    /// Handles the create account menu option.
    /// </summary>
    /// <returns>The user's password if creation is successful; otherwise, null.</returns>
    private static string? HandleCreateAccount()
    {
        return CreateAccount();
    }

    /// <summary>
    /// Handles the quit menu option.
    /// </summary>
    /// <returns>Never returns as the application exits.</returns>
    private static string? HandleQuit()
    {
        ExitApplication();
        return null;
    }

    /// <summary>
    /// Handles the account creation process.
    /// </summary>
    /// <returns>The user's password if creation is successful; otherwise, null.</returns>
    private static string? CreateAccount()
    {
        (string username, string password) = ConsoleUI.GetNewCredentials();
        try
        {
            s_currentUser = s_userService!.CreateUser(username, password);

            // Initialize the new user's vault
            string userDbPath = Path.Combine(s_dataDirectory, $"{username}.db");
            IDatabaseService userDbService = new DatabaseService(userDbPath);
            s_databaseServices.Add(userDbService);
            userDbService.InitializeVaultDatabase();

            AnsiConsole.MarkupLine("\n[Cyan1]Account successfully created. ＼ʕ •ᴥ•ʔ／[/]\n");
            return password;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[Red]Error: {ex.Message}[/]");
            s_currentUser = null;
            return null;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[Red]Error: {ex.Message}[/]");
            s_currentUser = null;
            return null;
        }
    }

    /// <summary>
    /// Handles the user login process, with a limited number of attempts.
    /// </summary>
    /// <returns>The user's password if login is successful; otherwise, null.</returns>
    private static string? PerformLogin()
    {
        // Allow up to configured number of login attempts
        for (int attempt = 1; attempt <= MAX_LOGIN_ATTEMPTS; attempt++)
        {
            (string username, string password) = ConsoleUI.GetLoginCredentials();
            s_currentUser = s_userService!.AuthenticateUser(username, password);

            if (s_currentUser != null)
            {
                AnsiConsole.MarkupLine("[Cyan1]Login successful! ദ്ദി ʕ•ᴥ• ʔ[/]\n\n\n");
                Thread.Sleep(500);
                return password;
            }

            int remainingAttempts = MAX_LOGIN_ATTEMPTS - attempt;
            AnsiConsole.MarkupLine(
                $"[Bold Red]Incorrect credentials. You have {remainingAttempts} attempts remaining.[/]"
            );
        }

        // Exit if login fails after max attempts
        AnsiConsole.MarkupLine("[Bold Red]Too many failed login attempts. Exiting.[/]");
        Environment.Exit(1);
        return null; // This line will not be reached but is required for compilation
    }

    /// <summary>
    /// The main application loop that runs after a user is logged in.
    /// </summary>
    /// <param name="sessionPassword">The user's raw password for this session.</param>
    private static void RunMainLoop(string sessionPassword)
    {
        // Derive the encryption key for the session upon successful login
        ICryptoService cryptoService = new CryptoService();
        byte[] encryptionKey = cryptoService.DeriveKeyFromPassword(
            sessionPassword,
            s_currentUser!.Salt
        );

        // Initialize services for the user's vault database
        string userDbPath = Path.Combine(s_dataDirectory, $"{s_currentUser.Username}.db");
        IDatabaseService userDbService = new DatabaseService(userDbPath);
        s_databaseServices.Add(userDbService);
        ICredentialService credentialService = new CredentialService(userDbService, cryptoService);

        while (true)
        {
            string choice = ConsoleUI.ShowMainMenu();
            ProcessMainMenuChoice(choice, credentialService, encryptionKey);
            ConsoleUI.ReturnToMenu();
        }
    }

    /// <summary>
    /// Processes the user's main menu selection.
    /// </summary>
    /// <param name="choice">The selected menu option.</param>
    /// <param name="credentialService">The credential service instance.</param>
    /// <param name="encryptionKey">The encryption key for the session.</param>
    private static void ProcessMainMenuChoice(
        string choice,
        ICredentialService credentialService,
        byte[] encryptionKey
    )
    {
        switch (choice)
        {
            case "Add new password":
                credentialService.AddCredential(encryptionKey);
                break;
            case "View full list of entries":
                credentialService.ViewAllCredentials(encryptionKey);
                break;
            case "Update an existing password":
                credentialService.UpdateCredential(encryptionKey);
                break;
            case "Delete an existing password":
                credentialService.DeleteCredential();
                break;
            case "Search for an existing password":
                credentialService.SearchCredentials(encryptionKey);
                break;
            case "Generate a random password":
                CredentialService.GenerateRandomPassword();
                break;
            case "Quit program":
                ExitApplication();
                break;
        }
    }

    /// <summary>
    /// Exits the application with a friendly message.
    /// </summary>
    private static void ExitApplication()
    {
        AnsiConsole.MarkupLine("\n[Blue]Closing databases...[/]");

        // Dispose all database services
        foreach (IDatabaseService dbService in s_databaseServices)
        {
            dbService.Dispose();
        }

        AnsiConsole.MarkupLine("[Blue]Exiting program... ʕ •ᴥ•ʔ[/]");
        Environment.Exit(0);
    }
}
