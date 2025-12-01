using SpotifyAPI.Web;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagLib;
using TagLibFile = TagLib.File;
using File = System.IO.File;

public class Config
{
    public List<string> Paths { get; set; } = new List<string> { };
    public bool LogSuccessfulMatches { get; set; } = false;
    public bool LogUnsuccessfulMatches { get; set; } = true;
    public bool LogProcessedPlaylists { get; set; } = true;
}

public static class Globals
{
    public static Config config { get; set; } = new Config()
    {
        Paths = new List<string>(["C:/Users/Public/Music", @"\\Network\Share\Path"]),
        LogSuccessfulMatches = false,
        LogUnsuccessfulMatches = false,
        LogProcessedPlaylists = false
    };
    public static string playlistInput = "";
    public static string configString = "";
}
public class Program
{


    private static readonly string playlistTemplate =
@"<?wpl version=""1.0""?>
<smil>
    <head>
        <meta name=""Generator"" content=""Microsoft Windows Media Player -- 12.0.19041.4842""/>
        <meta name=""ItemCount"" content=""{ItemCount}""/>
        <meta name=""ContentPartnerListID""/>
        <meta name=""ContentPartnerNameType""/>
        <meta name=""ContentPartnerName""/>
        <meta name=""Subtitle""/>
        <author/>
        <title>{PlaylistName}</title>
    </head>
    <body>
        <seq>
            {mediaElements}
        </seq>
    </body>
</smil>";

    private const string ConfigFileName = "zpotify.config";
    private const string PlaylistsFolder = "./playlists/";
    public static async Task Main(string[] args)
    {

        Console.Title = "Zpotify v1.1";

        var clientId = "f1f74e12dd8a4785ad3eaf9798fd270d";
        var clientSecret = "e2020e6febdc49919a8e8e274e841816";


        Console.WriteLine("                     ::::::::::                   ");
        Console.WriteLine("                  :::::::::::::::::               ");
        Console.WriteLine("              :----:::::::::::::::::::            ");
        Console.WriteLine("           :--------:-::::::-   :::::::::-        ");
        Console.WriteLine("        ---------  :----::::-       :::::::::     ");
        Console.WriteLine("     --------:    ----- :----          :::::::::: ");
        Console.WriteLine("   -------       -----  -----             ::::::::");
        Console.WriteLine("   -------     :-----   -----             ::::::::");
        Console.WriteLine("     ---------:-----    -----          :-::::::::-");
        Console.WriteLine("        -----------     -----      :-------:::::: ");
        Console.WriteLine("           .--------:   -----   ---------:----::  ");
        Console.WriteLine("           --------------------------:   -----:   ");
        Console.WriteLine("          -----   ----------------:     -----:    ");
        Console.WriteLine("         -----       :---------:       -----      ");
        Console.WriteLine("       :-----     ----------------:   -----       ");
        Console.WriteLine("      :=====   ---------------------------        ");
        Console.WriteLine("      ====::====----=   ----:   ---------:        ");
        Console.WriteLine("     ============       ----:     -----------     ");
        Console.WriteLine("   +=========-          ----:    -----:---------  ");
        Console.WriteLine("   =======              ----=   -----     :-------");
        Console.WriteLine("   =======              --===  -----      :-------");
        Console.WriteLine("     =========          -==== ==-=-    ---------: ");
        Console.WriteLine("        =========       ====-===== :=--------     ");
        Console.WriteLine("           ==========   ========-=======-:        ");
        Console.WriteLine("               ======================-            ");
        Console.WriteLine("                  ================-               ");
        Console.WriteLine("                     :=========                   ");
        Console.WriteLine("");
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine("");
        Console.WriteLine("                      Zpotify");
        Console.WriteLine("                  Spotify To Zune");
        Console.WriteLine("                 Playlist converter");
        Console.WriteLine("");
        Console.WriteLine("------------------------------------------------------------");

        ReadOrCreateConfigFile();

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            WriteColorLine("Spotify Client ID and Secret environment variables not set.", ConsoleColor.Red);
            return;
        }

        var spotifyClient = await GetSpotifyClient(clientId, clientSecret);
        if (spotifyClient == null)
        {
            WriteColorLine("Failed to authenticate with Spotify.", ConsoleColor.Red);
            return;
        }

        Globals.playlistInput = await waitForCommand();

        if (Globals.playlistInput.Contains(","))
        {
            Globals.playlistInput = Globals.playlistInput.Replace("\r", "").Replace("\n", "").Replace(" ", "");

            string[] playlists = Globals.playlistInput.Split(',');
            foreach (var playlist in playlists)
            {
                await processPlaylist(playlist, spotifyClient, Globals.config.Paths);
                if (Globals.config.LogProcessedPlaylists == true)
                {
                    await logPlaylist(playlist, spotifyClient);
                }
            }
        }
        else
        {
            await processPlaylist(Globals.playlistInput, spotifyClient, Globals.config.Paths);
            await logPlaylist(Globals.playlistInput, spotifyClient);
        }

        WriteColorLine("\nPress 'c' to close the application, press 'Enter' to continue...", ConsoleColor.Yellow);
        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
        if (keyInfo.Key == ConsoleKey.C)
        {
            Environment.Exit(0);
        }
        Globals.playlistInput = "";
        await Main(args);

    }
    private delegate Task CommandHandler(string commandKey, string arg);

    private static readonly Dictionary<string, CommandHandler> CommandHandlers = new()
    {
        { "help", async (key, arg) =>
            {
                string helpText = "Below is a list of options and information availble in this program:\n\n" +
                    "spotify playlist url/id    -   Entering a single playlist URL or ID will get that playlist. Alternatively, a list of URLs or IDs can be provided with commas separating the items\n\n" +
                    "help                       -   Displays this info\n\n" +
                    "config                     -   Display the config file in the console\n" +
                    "config -open               -   Open the config file\n" +
                    "config -logmatched         -   Toggle whether to create a log file with a list of matched tracks from a playlist\n" +
                    "config -logunmatched       -   Toggle whether to create a log file with a list of unmatched tracks from a playlist.\n" +
                    "config -logplaylists       -   Toggle logging processed playlists\n" +
                    "config -rebuild            -   Rebuild the config file to the default file. Used for fixing config errors.\n\n" +
                    $"-l                         -   Displays a list of playlists in the playlists output folder {PlaylistsFolder}. \n\n" +
                    "-p                         -   Displays the path(s) in the config file.\n" +
                    "-p add <path>              -   Add a path to the config file.\n" +
                    "-p remove <path>           -   Remove a path from the config file.\n" +
                    "-p remove all              -   Remove all paths from config file.\n\n" +
                    "-f <path>                  -   Retrieves a list of playlists from a file\n\n" +
                    "clear                      -   Clear all the text from the console\n\n" +
                    "exit/quit                  -   Closes this program.";
                Console.WriteLine("\n");
                WriteColorLine(helpText, ConsoleColor.Cyan);
                WriteColorLine("Slick newbie. A kurius egg you are.", ConsoleColor.Black);
                Console.WriteLine("\n");
                await Task.CompletedTask;
            }
        },
        { "-l", async (key, arg) =>
            {
                if (Directory.Exists(PlaylistsFolder)) {
                    string[] wplFiles = Directory.GetFiles(PlaylistsFolder, "*.wpl");
                    if (wplFiles.Length != 0)
                    {
                        foreach (var file in wplFiles)
                        {
                            WriteColorLine($"{file}", ConsoleColor.Yellow);
                        }
                    }
                    else
                    {
                        WriteColorLine("No playlists found in playlist folder.", ConsoleColor.Yellow);
                    }

                }
                else
                {
                    WriteColorLine("Playlist folder not found. Process a playlist to create the folder.",ConsoleColor.Yellow);
                }
                await Task.CompletedTask;
            }
        },
        { "-p", async (key, arg) =>
            {
                await DisplayConfigPaths();
            }
        },
        { "-p add", async (key, path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await AddPathToConfigFile(path);
                }
                else
                {
                    WriteColorLine($"Usage: {key} <path>", ConsoleColor.Red);
                }
            }
        },
        { "-p remove", async (key, path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await RemovePathFromConfigFile(path);
                }
                else
                {
                    WriteColorLine($"Usage: {key} <path>", ConsoleColor.Red);
                }
            }
        },
        { "why", async (key, arg) =>
            {
                var whyStr = "I'm honestly just sick of smart phones.";
                WriteColorLine(whyStr, ConsoleColor.Yellow);
                await Task.CompletedTask;
            }
        },
        { "42", async (key, arg) =>
            {
                WriteColorLine("Life, universe, etc.", ConsoleColor.Yellow);
                await Task.CompletedTask;
            }
        },
        { "exit", async (key, arg) => { Environment.Exit(0); await Task.CompletedTask; } },
        { "quit", async (key, arg) => { Environment.Exit(0); await Task.CompletedTask; } },
        { "snake", async (key, arg) =>
            {
                WriteColorLine("A snake?! Where?! In my boot?!?!", ConsoleColor.Yellow);
                await Task.Delay(3000);
                Console.Clear();
                WriteColorLine("Say nothing of this game, but do mention the BigFoot.", ConsoleColor.DarkGreen);
                WriteColorLine("Credit for the base game: https://github.com/dotnet/dotnet-console-games", ConsoleColor.Magenta);

                string titleScreen = "                                                             \r\n888888888888                           88                    \r\n         ,88                           88                    \r\n       ,88\"                            88                    \r\n     ,88\"     8b,dPPYba,   ,adPPYYba,  88   ,d8   ,adPPYba,  \r\n   ,88\"       88P'   `\"8a  \"\"     `Y8  88 ,a8\"   a8P_____88  \r\n ,88\"         88       88  ,adPPPPP88  8888[     8PP\"\"\"\"\"\"\"  \r\n88\"           88       88  88,    ,88  88`\"Yba,  \"8b,   ,aa  \r\n888888888888  88       88  `\"8bbdP\"Y8  88   `Y8a  `\"Ybbd8\"'  \r\n                                                             \r\n                                                             ";

                WriteColorLine(titleScreen,ConsoleColor.Green);

                await Task.Delay(1000);
                await RunSnakeGame();
                Console.WriteLine("\n");
            }
        },
        { "snakeur", async (key, arg) => { WriteColorLine("What's that?", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "easter egg", async (key, arg) => { WriteColorLine("There are no easter eggs.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "zune", async (key, arg) =>
            {
                WriteColorLine(" ______________________________ \n" +
                            "|\\___________ === --________(@)_\\\n" +
                            "| | ____________________________ | \n" +
                            "| | |||||||||||||||||||||||||||| | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | ||                        || | \n" +
                            "| | || ______________________ || | \n" +
                            "| | |||||||||||||||||||||||||||| | \n" +
                            "| |                              | \n" +
                            "| |              @@@@            | \n" +
                            "| |           @@@@@@@@@@         | \n" +
                            "| |          @@@@@@@@@@@@        | \n" +
                            "| |    @    @@@@@@@@@@@@@@    @  | \n" +
                            "| |   @@@   @@@@@@@@@@@@@@   @@@ | \n" +
                            "| |    @     @@@@@@@@@@@@     @  | \n" +
                            "| |           @@@@@@@@@@         | \n" +
                            "| |              @@@@            | \n" +
                            "| |                              | \n" +
                            " \\| ____________________________ | \n" +
                            "Zoon", ConsoleColor.Yellow);
                await Task.CompletedTask;
            }
        },
        { "ms-dos", async (key, arg) => { WriteColorLine("Volume in drive A has no label.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "3.1", async (key, arg) => { WriteColorLine("Humble beginnings.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "95", async (key, arg) => { WriteColorLine("The color you're loooking for is #00807F", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "98", async (key, arg) => { WriteColorLine("Could you imagine standing in line for a day and a half just to pick up a copy of Windows 98? I could.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "xp", async (key, arg) => { WriteColorLine("In some versions of XP you could press Ctrl+alt+delete on the lock screen and access another login menu where you could enter admin / password and log in as an admin....I still haven't told my sister that I was the one who killed her laptop.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "m.e.", async (key, arg) => { WriteColorLine("Idk man, you call anything Millenium and I'm buing it. Windows, Backstreet Boys, whatever. Y2K Futurism was rad.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "2000", async (key, arg) => { WriteColorLine("I literally have no idea what the difference between XP, 2000, and M.E. are. How are they not just the same OS?", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "vista", async (key, arg) => { WriteColorLine("You could literally cruiiiise the vistaaaas.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "7", async (key, arg) => { WriteColorLine("Windows 7 Ultimate is the greatest desktop OS ever created.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "8", async (key, arg) => { WriteColorLine("I like how normally an update isn't a big deal but 8.1 got it's own sticker.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "9", async (key, arg) => { WriteColorLine("Win10 == Win9", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "10", async (key, arg) => { WriteColorLine("The sinking of the Titanic started with the illusion that it was unsinkable.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "11", async (key, arg) => { WriteColorLine("Remember when they said 10 was going to be the final version of Windows?", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "linux", async (key, arg) => { WriteColorLine("Tux good.", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "mac", async (key, arg) => { WriteColorLine("and cheese", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "apple", async (key, arg) => { WriteColorLine("pie", ConsoleColor.Yellow); await Task.CompletedTask; } },
        { "zpotify", async (key, arg) => { WriteColorLine("What?", ConsoleColor.Yellow); await Task.CompletedTask; } },

        { "config", async (key, arg) => { await printConfig(); await Task.CompletedTask;} },
        { "config -open", async (key, arg) => { await LaunchConfig(); await Task.CompletedTask; } },
        { "config -logmatched", async (key, arg) =>
            {
                Globals.config.LogSuccessfulMatches = !Globals.config.LogSuccessfulMatches;
                updateConfigFile();
                WriteColorLine($"Succesful match logging marked as {Globals.config.LogSuccessfulMatches}", ConsoleColor.Yellow);

            }
        },
        { "config -logunmatched", async (key, arg) =>
            {
                Globals.config.LogUnsuccessfulMatches = !Globals.config.LogUnsuccessfulMatches;
                updateConfigFile();
                WriteColorLine($"Unsuccesful match logging marked as {Globals.config.LogUnsuccessfulMatches}", ConsoleColor.Yellow);

            }
        },

        { "config -rebuild", async (key, arg) =>
            {
                if (File.Exists(ConfigFileName))
                {
                    File.Delete(ConfigFileName);
                }
                ReadOrCreateConfigFile();
                await Task.CompletedTask;
                WriteColorLine("The config file has been rebuilt. Update your paths with -p.", ConsoleColor.Yellow);
            }
        },
        { "config -logplaylists", async (key, arg) =>
            {
                Globals.config.LogProcessedPlaylists = !Globals.config.LogProcessedPlaylists;
                updateConfigFile();
                WriteColorLine($"Processed playlist logging marked as {Globals.config.LogProcessedPlaylists}", ConsoleColor.Yellow);
            }
        },
        { "clear", async (key, arg) => { Console.Clear(); await Task.CompletedTask; } },

        { "-f", async (key, path) =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    var outStr = await processFile(path);
                    await Task.CompletedTask;
                    Globals.playlistInput = outStr;
                }
                else
                {
                    WriteColorLine($"Usage: {key} <path>", ConsoleColor.Red);
                }
            }
        }

    };

    private static async Task logPlaylist(string playlist, SpotifyClient spotifyClient)
    {
        string playlistLog = "./playlists/_log.txt";

        var playlistId = ParsePlaylistId(playlist);
        var playlistData = await GetPlaylistData(spotifyClient, playlistId);

        File.AppendAllText(playlistLog, $"{playlist} | {playlistData.Value.Name}\n");
    }


    private static async Task<string> processFile(string filePath)
    {
        string fileText = File.ReadAllText(filePath);

        string[] filePaths = fileText.Split(
            new string[] { Environment.NewLine },
            StringSplitOptions.None
        );

        var outStr = "";

        foreach (var link in filePaths)
        {
            outStr += link + ',';
        }
        outStr = outStr.Trim();
        outStr = outStr.Substring(0, outStr.Length - 1);
        return outStr;
    }

    private static async Task LaunchConfig()
    {
        Process.Start("notepad.exe", ConfigFileName);

    }
    private static async Task printConfig()
    {
        string configStr = File.ReadAllText(ConfigFileName);
        WriteColorLine("Note: network paths will display as the program sees them, not as they are read. Extra \\ characters is expected.\nIt is recommended to use -p to view and update your paths.\n", ConsoleColor.Cyan);
        WriteColorLine(configStr, ConsoleColor.Yellow);
    }
    private static async Task<string> waitForCommand()
    {
        bool exitloop = false;
        while (exitloop == false)
        {
            if (Globals.playlistInput == "")
            {
                WriteColorLine("Enter Spotify playlist URL, ID, or HELP for a list of options: ", ConsoleColor.Green);
                Globals.playlistInput = Console.ReadLine();
            }


            if (string.IsNullOrEmpty(Globals.playlistInput))
            {
                WriteColorLine("No input detected. Try again...", ConsoleColor.Yellow);
                Console.WriteLine("\n");
                continue;
            }

            var trimmedInput = Globals.playlistInput.Trim();
            var lowerInput = trimmedInput.ToLower();

            var parts = lowerInput.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

            string commandKey = string.Empty;
            string argument = string.Empty;

            if (parts.Length >= 2)
            {
                string potentialTwoWordKey = parts[0] + " " + parts[1];

                if (CommandHandlers.TryGetValue(potentialTwoWordKey, out var handlerTwoWord))
                {
                    commandKey = potentialTwoWordKey;

                    int firstSpaceIndex = trimmedInput.IndexOf(' ');
                    int secondSpaceIndex = firstSpaceIndex != -1 ? trimmedInput.IndexOf(' ', firstSpaceIndex + 1) : -1;

                    if (secondSpaceIndex != -1)
                    {
                        argument = trimmedInput.Substring(secondSpaceIndex).Trim();
                    }
                    else
                    {
                        argument = string.Empty;
                    }

                    await handlerTwoWord(commandKey, argument);
                    if (!Globals.playlistInput.Contains("-f"))
                    {
                        Globals.playlistInput = "";
                    }

                    continue;
                }
            }

            if (parts.Length >= 1)
            {
                string singleWordKey = parts[0];

                if (CommandHandlers.TryGetValue(singleWordKey, out var handlerSingleWord))
                {
                    commandKey = singleWordKey;

                    int firstSpaceIndex = trimmedInput.IndexOf(' ');

                    if (firstSpaceIndex != -1)
                    {
                        argument = trimmedInput.Substring(firstSpaceIndex).Trim();
                    }
                    else
                    {
                        argument = string.Empty;
                    }

                    if (Globals.playlistInput.Contains("-f"))
                    {
                        await handlerSingleWord(commandKey, argument);
                    }
                    else
                    {
                        await handlerSingleWord(commandKey, argument);
                        Globals.playlistInput = "";
                    }
                    continue;
                }
            }

            string spotifyPlaylistUrl = "https://open.spotify.com/playlist/";
            if (trimmedInput.Contains("spotify.com") || trimmedInput.Length > spotifyPlaylistUrl.Length)
            {
                exitloop = true;
            }
            else if (trimmedInput.Length == 22)
            {
                exitloop = true;
            }
            else
            {
                WriteColorLine($"'{Globals.playlistInput}' is not a recognized command or valid Spotify URL/ID. Try again.", ConsoleColor.Red);
                Console.WriteLine("\n");
                Globals.playlistInput = "";
                continue;
            }
        }
        return Globals.playlistInput;
    }


    private static async Task<string> getPathsFromString()
    {
        var pathsStr = Globals.configString;
        pathsStr = Regex.Replace(pathsStr, @".*?\[(.*)\].*", "$1", RegexOptions.Singleline);
        pathsStr = pathsStr.Replace("\"", "");
        string[] pathsArr = pathsStr.Split(",");
        pathsStr = "";
        foreach (var path in pathsArr)
        {
            string thisPath = path.Trim();
            pathsStr = pathsStr + thisPath + "\n";
        }
        return pathsStr;
    }

    private static async Task DisplayConfigPaths()
    {
        ReadOrCreateConfigFile();
        string paths = await getPathsFromString();

        if (!string.IsNullOrEmpty(paths))
        {
            WriteColorLine("\nConfigured Music Paths:", ConsoleColor.Cyan);
            WriteColorLine(paths, ConsoleColor.Yellow);
        }
        else
        {
            WriteColorLine("No paths configured in the config file.", ConsoleColor.Yellow);
        }
        await Task.CompletedTask;
    }

    private static async Task AddPathToConfigFile(string path)
    {
        var normalizedPath = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var existingPaths = Globals.config.Paths ?? new List<string>();

        if (existingPaths.Any(p => p.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
        {
            WriteColorLine($"\nPath already exists in {ConfigFileName}: {normalizedPath}", ConsoleColor.Yellow);
            return;
        }

        Globals.config.Paths.Add(normalizedPath);

        updateConfigFile();
        WriteColorLine($"\nSuccessfully added path to {ConfigFileName}: {normalizedPath}", ConsoleColor.Cyan);

        WriteColorLine("Verifying access to the path...", ConsoleColor.Cyan);

        if (!Directory.Exists(normalizedPath))
        {
            WriteColorLine("WARNING: The added path does not appear to exist or is inaccessible. Please verify it.\n", ConsoleColor.Red);
        }
        else
        {
            WriteColorLine("Path verified successfully.\n", ConsoleColor.Cyan);

        }
    }

    private static async Task RemovePathFromConfigFile(string path)
    {
        if (path.ToLower() == "all")
        {
            while (Globals.config.Paths.Count > 0)
            {
                var curPath = Globals.config.Paths[0];
                Globals.config.Paths.RemoveAt(0);
                WriteColorLine($"Path {curPath} removed successfully.\n", ConsoleColor.Cyan);
            }
            WriteColorLine($"All paths removed successfully.\n", ConsoleColor.Cyan);
            updateConfigFile();
        }
        else if (!Globals.config.Paths.Contains(path))
        {
            WriteColorLine($"\nError: 'Paths' key not found in {ConfigFileName}.", ConsoleColor.Red);
            return;
        }
        else
        {
            Globals.config.Paths.Remove(path);
            WriteColorLine($"Path {path} removed successfully.\n", ConsoleColor.Cyan);
            updateConfigFile();
        }
    }

    public static Config ReadOrCreateConfigFile()
    {
        var configs = new List<string>();

        if (!File.Exists(ConfigFileName))
        {
            WriteColorLine($"Config file '{ConfigFileName}' not found. Creating default...\n", ConsoleColor.Red);
            string userPath = "";
            while (!Path.Exists(userPath))
            {
                WriteColorLine("Provide a path to your music folder. If you have more than one, provide one, then later use the '-p add' command to add a second one:", ConsoleColor.Cyan);
                userPath = Console.ReadLine();
                if (!Path.Exists(userPath))
                {
                    WriteColorLine($"Error: The path {userPath} does not exist.\n", ConsoleColor.Red);
                }
            }
            Globals.config.Paths = [userPath];
            Globals.config.LogSuccessfulMatches = false;
            Globals.config.LogUnsuccessfulMatches = true;
            Globals.config.LogProcessedPlaylists = true;

            updateConfigFile();
            WriteColorLine("\nConfig file has been created.\n\nFor more info on config files use 'help'.\n", ConsoleColor.Cyan);
        }
        return getConfigFile();
    }

    public static Config updateConfigFile()
    {
        string jsonString = JsonSerializer.Serialize(Globals.config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFileName, jsonString);
        return Globals.config;
    }
    public static Config getConfigFile()
    {
        Console.WriteLine($"Reading config file '{ConfigFileName}'.");
        var fileText = File.ReadAllText(ConfigFileName);
        var deserialized = JsonSerializer.Deserialize<Config>(fileText);
        string deserialStr = fileText;
        deserialStr = deserialStr.Replace("\\\\", "\\");
        Globals.config = deserialized;
        Globals.configString = deserialStr;
        return Globals.config;
    }

    public static void WriteColorLine(string text, ConsoleColor color)
    {
        ConsoleColor originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }

    private static async Task processPlaylist(string playlistInput, SpotifyClient spotifyClient, List<string> musicFolderPaths)
    {

        var playlistId = ParsePlaylistId(playlistInput);

        Console.WriteLine("Getting playlist data...");

        var playlistData = await GetPlaylistData(spotifyClient, playlistId);

        if (playlistData == null || !playlistData.Value.Tracks.Any())
        {
            WriteColorLine("Could not fetch playlist data or playlist is empty.", ConsoleColor.Yellow);
            return;
        }

        var (playlistName, spotifyTracks) = playlistData.Value;
        var sanitizedPlaylistName = SanitizeFileName(playlistName);
        var matchedTracks = new List<(string SpotifyArtist, string SpotifyTrack, string FilePath)>();
        var foundSpotifyTracks = new HashSet<string>();

        foreach (var folderPath in musicFolderPaths)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                WriteColorLine($"Skipping invalid or inaccessible folder path: {folderPath}", ConsoleColor.Yellow);
                continue;
            }
            Console.WriteLine($"Checking path for tracks: {folderPath}...");
            matchedTracks.AddRange(MatchTracks(spotifyTracks, folderPath, foundSpotifyTracks));
        }
        var unmatchedTracks = spotifyTracks
            .Where(t => !foundSpotifyTracks.Contains(t.Name + t.Artist))
            .ToList();

        await OutputMatchedTracks(matchedTracks, sanitizedPlaylistName);
        await OutputUnmatchedTracks(unmatchedTracks, sanitizedPlaylistName);
        await OutputWplPlaylist(matchedTracks, playlistName, sanitizedPlaylistName);
    }

    private static async Task<SpotifyClient> GetSpotifyClient(string clientId, string clientSecret)
    {
        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));

        return new SpotifyClient(config);
    }
    private static string ParsePlaylistId(string urlOrId)
    {
        var regex = new Regex(@"playlist\/(?<id>[a-zA-Z0-9]+)");
        var match = regex.Match(urlOrId);
        return match.Success ? match.Groups["id"].Value : urlOrId;
    }
    private static async Task<(string Name, List<(string Artist, string Name)> Tracks)?> GetPlaylistData(SpotifyClient spotifyClient, string playlistId)
    {
        try
        {
            var playlist = await spotifyClient.Playlists.Get(playlistId);
            var playlistName = playlist.Name;
            var tracks = new List<(string, string)>();
            var paging = await spotifyClient.Playlists.GetItems(playlistId);

            await foreach (var item in spotifyClient.Paginate(paging))
            {
                if (item.Track is FullTrack track)
                {
                    var artist = track.Artists.FirstOrDefault()?.Name ?? "Unknown Artist";
                    tracks.Add((artist, track.Name));
                }
            }
            return (playlistName, tracks);
        }
        catch (APIException ex)
        {
            WriteColorLine($"Spotify API Error: {ex.Message}", ConsoleColor.Red);
            return null;
        }
    }

    private static List<(string SpotifyArtist, string SpotifyTrack, string FilePath)> MatchTracks(
    List<(string Artist, string Name)> spotifyTracks,
    string folderPath,
    HashSet<string> foundSpotifyTracks)
    {
        var matchedTracks = new List<(string, string, string)>();

        WriteColorLine("Audio file types compatible with Zune Software: .mp3, .aac, .mp4, .m4b, .mov, .wma", ConsoleColor.White);
        Console.WriteLine("Scanning directory for files...");
        var filePaths = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.EndsWith(".mp3") || s.EndsWith(".aac") || s.EndsWith(".mp4") || s.EndsWith(".m4b") || s.EndsWith(".mov") || s.EndsWith(".wma"))
            .ToList();

        Console.WriteLine($"\rFound {filePaths.Count} local files. Starting match process...");

        int tracksProcessed = 0;

        foreach (var spotifyTrack in spotifyTracks)
        {
            tracksProcessed++;
            Console.Write($"\r{tracksProcessed}/{spotifyTracks.Count}");

            var possibleFileMatches = new List<string>();

            string normalizedSpotifyName = NormalizeSpotifyTrackName(spotifyTrack.Name);
            string spotifyTrackName = CleanString(normalizedSpotifyName);
            string spotifyArtist = CleanString(spotifyTrack.Artist).ToLower();

            bool requiresAcousticInPath = spotifyTrack.Name.IndexOf("acoustic", StringComparison.OrdinalIgnoreCase) >= 0;
            bool requiresLiveInPath = spotifyTrack.Name.IndexOf("live", StringComparison.OrdinalIgnoreCase) >= 0;

            foreach (var file in filePaths)
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(file) ?? "";
                string matchFileName = CleanString(fileNameNoExt);

                string matchFileFullPathCleaned = CleanString(file);

                bool meetsAcousticRequirement = !requiresAcousticInPath || file.IndexOf("acoustic", StringComparison.OrdinalIgnoreCase) >= 0;
                bool meetsLiveRequirement = !requiresLiveInPath || file.IndexOf("live", StringComparison.OrdinalIgnoreCase) >= 0;

                if (matchFileFullPathCleaned.Contains(spotifyArtist) && matchFileName.Contains(spotifyTrackName)
                    && meetsAcousticRequirement && meetsLiveRequirement)
                {
                    possibleFileMatches.Add(file);
                }
            }

            if (possibleFileMatches.Count > 1)
            {
                possibleFileMatches = FilterMatchesByVersion(possibleFileMatches, spotifyTrack.Name);

                if (possibleFileMatches.Count > 1)
                {
                    possibleFileMatches = FilterMatchesByTag(possibleFileMatches, spotifyTrack.Artist, spotifyTrack.Name);
                }
            }


            var keptFile = "";

            if (possibleFileMatches.Count == 1)
            {
                keptFile = possibleFileMatches[0];
            }
            else if (possibleFileMatches.Count > 1)
            {
                WriteColorLine($"\nMultiple files were found for {spotifyTrack.Artist} - {spotifyTrack.Name}. Choose which one to use:", ConsoleColor.Cyan);
                int matchCount = 1;
                foreach (var match in possibleFileMatches)
                {
                    WriteColorLine($"{matchCount}: {match}", ConsoleColor.Yellow);
                    matchCount++;
                }
                WriteColorLine($"0: None", ConsoleColor.Yellow);
                Console.WriteLine("");

                int selection = 0;
                Console.Write("Selection: ");
                while (!int.TryParse(Console.ReadLine(), out selection) || selection < 0 || selection > possibleFileMatches.Count)
                {
                    Console.WriteLine("Invalid input. Please enter a valid number:");
                    Console.Write("Selection: ");
                }

                if (selection == 0)
                {
                    keptFile = "none";
                }
                else
                {
                    keptFile = possibleFileMatches[selection - 1];
                }
            }

            if (!String.IsNullOrEmpty(keptFile) && keptFile != "none")
            {
                matchedTracks.Add((spotifyTrack.Artist, spotifyTrack.Name, keptFile));
                foundSpotifyTracks.Add(spotifyTrack.Name + spotifyTrack.Artist);
            }
            else if (keptFile == "none" || possibleFileMatches.Count == 0)
            {
                WriteColorLine($"Track {spotifyTrack.Artist} - {spotifyTrack.Name} skipped.\n", ConsoleColor.Cyan);
            }
        }

        WriteColorLine($"\rMatching complete! Found {matchedTracks.Count} matches in this folder.             ", ConsoleColor.Green);

        return matchedTracks;
    }

    private static List<string> FilterMatchesByVersion(List<string> potentialMatches, string spotifyTrackName)
    {
        var filteredMatches = new List<string>(potentialMatches);

        bool spotifyIsAcoustic = spotifyTrackName.IndexOf("acoustic", StringComparison.OrdinalIgnoreCase) >= 0;
        bool spotifyIsLive = spotifyTrackName.IndexOf("live", StringComparison.OrdinalIgnoreCase) >= 0;

        var keepList = new List<string>();

        foreach (var filePath in filteredMatches)
        {
            bool fileIsAcoustic = filePath.IndexOf("acoustic", StringComparison.OrdinalIgnoreCase) >= 0;
            bool fileIsLive = filePath.IndexOf("live", StringComparison.OrdinalIgnoreCase) >= 0;

            if (spotifyIsAcoustic)
            {
                if (fileIsAcoustic)
                {
                    keepList.Add(filePath);
                }
            }
            else
            {
                if (!fileIsAcoustic)
                {
                    if (spotifyIsLive)
                    {
                        if (fileIsLive)
                        {
                            keepList.Add(filePath);
                        }
                    }
                    else
                    {
                        if (!fileIsLive)
                        {
                            keepList.Add(filePath);
                        }
                    }
                }
            }
        }

        if (potentialMatches.Count > keepList.Count)
        {
            WriteColorLine($"\nReduced {potentialMatches.Count} duplicate matches to {keepList.Count} by filtering versions ('Acoustic'/'Live').", ConsoleColor.DarkYellow);
        }

        if (keepList.Count == 0 && potentialMatches.Count > 0)
        {
            return potentialMatches;
        }

        return keepList;
    }


    private static List<string> FilterMatchesByTag(
    List<string> potentialMatches,
    string spotifyArtist,
    string spotifyTrackName)
    {
        var filteredMatches = new List<string>();

        string normalizedSpotifyName = NormalizeSpotifyTrackName(spotifyTrackName);

        string targetArtist = CleanString(spotifyArtist).ToLower();
        string targetTrack = CleanString(normalizedSpotifyName);

        foreach (var filePath in potentialMatches)
        {
            try
            {
                using (var file = TagLibFile.Create(filePath))
                {
                    string fileArtist = file.Tag.Performers.FirstOrDefault() ?? "";
                    string fileTitle = file.Tag.Title ?? "";

                    string cleanFileArtist = CleanString(fileArtist).ToLower();
                    string cleanFileTitle = CleanString(fileTitle);

                    bool artistMatch = cleanFileArtist.Contains(targetArtist);

                    bool titleMatch = cleanFileTitle.Contains(targetTrack) || targetTrack.Contains(cleanFileTitle);

                    if (artistMatch && titleMatch)
                    {
                        filteredMatches.Add(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nWarning: Could not read tags for file {filePath}. Error: {ex.Message}");
            }
        }

        if (potentialMatches.Count > 1)
        {
            WriteColorLine($"\nReduced {potentialMatches.Count} potential matches to {filteredMatches.Count} using ID3 tag filtering.", ConsoleColor.DarkYellow);
        }

        return filteredMatches;
    }

    private static string CleanString(string input)
    {
        string normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            if (char.IsLetterOrDigit(c) || c == ' ')
            {
                sb.Append(c);
            }
            else if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.SpacingCombiningMark)
            {
                continue;
            }
        }
        string cleanInput = sb.ToString();

        if (!Regex.IsMatch(cleanInput, "[a-zA-Z0-9]"))
        {
            return input.Trim().ToLower();
        }

        cleanInput = cleanInput.ToLower()
                               .Replace("the", "")
                               .Replace("&", "and")
                               .Trim();

        cleanInput = Regex.Replace(cleanInput, @"[^a-z0-9 ]", "");

        cleanInput = Regex.Replace(cleanInput, @"\b(and)\b", " ");
        cleanInput = Regex.Replace(cleanInput, @"\s+", " ").Trim();

        return cleanInput;
    }

    private static string NormalizeSpotifyTrackName(string trackName)
    {
        string cleaned = trackName;

        string pattern = @"\s*(-|\/)\s*(\d{4})?\s*(re-?master(ed)?|remaster(ed)|remastered|re-mastered|version|release|edit|mix|live|explicit|single( ?version)?|original|mono|stereo|album|radio)\s*(\d{4})?\s*(\s*(-|\/)\s*(\d{4})?)*\s*";
        cleaned = Regex.Replace(cleaned, pattern, " ", RegexOptions.IgnoreCase).Trim();

        string metadataInBracketsPattern = @"\s*(\(|\[).*?(\d{4}|\b(re-?master(ed)?|remaster(ed)|remastered|re-mastered|version|release|edit|mix|live|explicit|single|original)\b).*?(\)|\])\s*";
        cleaned = Regex.Replace(cleaned, metadataInBracketsPattern, " ", RegexOptions.IgnoreCase).Trim();

        cleaned = Regex.Replace(cleaned, @"\s*(\(|\[)[^)]*(\)|])\s*", " ", RegexOptions.IgnoreCase).Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    private static string SanitizeFileName(string playlistName)
    {
        var sanitizedName = playlistName;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitizedName = sanitizedName.Replace(invalidChar, '_');
        }
        return sanitizedName;
    }

    private static async Task OutputMatchedTracks(
        List<(string SpotifyArtist, string SpotifyTrack, string FilePath)> matchedTracks,
        string sanitizedPlaylistName)
    {
        if (matchedTracks.Any())
        {


            var lines = matchedTracks.Select(t => $"{t.SpotifyArtist} | {t.SpotifyTrack} | {t.FilePath}");
            Directory.CreateDirectory(PlaylistsFolder);
            Console.WriteLine($"\nSuccessfully found {matchedTracks.Count} matches!");

            if (Globals.config.LogSuccessfulMatches == true)
            {
                var outputFileName = Path.Combine(PlaylistsFolder, $"{sanitizedPlaylistName}-matched.txt");
                await File.WriteAllLinesAsync(outputFileName, lines);
                Console.WriteLine($"\nSuccessfully wrote {matchedTracks.Count} matches to {outputFileName}!");
            }
        }
        else
        {
            Console.WriteLine("No tracks matched local files for matched tracks output.");
        }
    }
    private static async Task OutputUnmatchedTracks(
        List<(string Artist, string Name)> unmatchedTracks,
        string sanitizedPlaylistName)
    {
        if (unmatchedTracks.Any())
        {
            var lines = unmatchedTracks.Select(t => $"{t.Artist} | {t.Name}");
            Directory.CreateDirectory(PlaylistsFolder);
            Console.WriteLine($"\nFound {unmatchedTracks.Count} unmatched tracks!");

            if (Globals.config.LogUnsuccessfulMatches == true)
            {
                var outputFileName = Path.Combine(PlaylistsFolder, $"{sanitizedPlaylistName}-unmatched.txt");
                await File.WriteAllLinesAsync(outputFileName, lines);
                Console.WriteLine($"\nSuccessfully wrote {unmatchedTracks.Count} unmatched tracks to {outputFileName}");
            }
        }
        else
        {
            Console.WriteLine("All tracks were successfully matched! No unmatched tracks file generated.");
        }
    }

    private static string EscapeXmlAttribute(string value)
    {
        return value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static async Task OutputWplPlaylist(
        List<(string SpotifyArtist, string SpotifyTrack, string FilePath)> matchedTracks,
        string playlistName,
        string sanitizedPlaylistName)
    {
        if (!matchedTracks.Any())
        {
            return;
        }

        var playlistMedia = new StringBuilder();
        foreach (var track in matchedTracks)
        {
            var encodedPath = EscapeXmlAttribute(track.FilePath);
            playlistMedia.AppendLine($"            <media src=\"{encodedPath}\"/>");
        }

        var finalWplContent = playlistTemplate
            .Replace("{PlaylistName}", EscapeXmlAttribute(playlistName))
            .Replace("{ItemCount}", matchedTracks.Count.ToString())
            .Replace("{mediaElements}", playlistMedia.ToString());

        var outputFileName = Path.Combine(PlaylistsFolder, $"{sanitizedPlaylistName}.wpl");
        Directory.CreateDirectory(PlaylistsFolder);
        await File.WriteAllTextAsync(outputFileName, finalWplContent);

        Console.WriteLine($"Successfully created WPL playlist: {outputFileName}");
    }

    private enum Direction
    {
        Up = 0,
        Down = 1,
        Left = 2,
        Right = 3,
    }

    private enum Tile
    {
        Open = 0,
        Snake,
        Food,
    }

    private static void GetDirection(ref Direction? direction, ref bool closeRequested)
    {
        switch (Console.ReadKey(true).Key)
        {
            case ConsoleKey.UpArrow: direction = Direction.Up; break;
            case ConsoleKey.DownArrow: direction = Direction.Down; break;
            case ConsoleKey.LeftArrow: direction = Direction.Left; break;
            case ConsoleKey.RightArrow: direction = Direction.Right; break;
            case ConsoleKey.Escape: closeRequested = true; break;
        }
    }

    private static void PositionFood(Tile[,] map, int width, int height)
    {
        List<(int X, int Y)> possibleCoordinates = new();
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (map[i, j] is Tile.Open)
                {
                    possibleCoordinates.Add((i, j));
                }
            }
        }

        if (!possibleCoordinates.Any())
        {
            return;
        }

        int index = Random.Shared.Next(possibleCoordinates.Count);
        (int X, int Y) = possibleCoordinates[index];
        map[X, Y] = Tile.Food;
        Console.SetCursorPosition(X, Y);
        Console.Write('Z');
    }

    private static async Task RunSnakeGame()
    {
        Exception? exception = null;
        int speedInput;
        string prompt = $"Select speed [1], [2] (default), or [3]: ";
        string? input;

        Console.Write(prompt);
        while (!int.TryParse(input = Console.ReadLine(), out speedInput) || speedInput < 1 || 3 < speedInput)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                speedInput = 2;
                break;
            }
            else
            {
                Console.WriteLine("Invalid Input. Try Again...");
                Console.Write(prompt);
            }
        }
        int[] velocities = [100, 70, 50];
        int velocity = velocities[speedInput - 1];
        char[] DirectionChars = ['^', 'v', '<', '>',];
        TimeSpan sleep = TimeSpan.FromMilliseconds(velocity);

        int width = Console.WindowWidth;
        int height = Console.WindowHeight;
        Tile[,] map = new Tile[width, height];
        Direction? direction = null;
        Queue<(int X, int Y)> snake = new();
        (int X, int Y) = (width / 2, height / 2);
        bool closeRequested = false;

        try
        {
            Console.CursorVisible = false;
            Console.Clear();
            snake.Enqueue((X, Y));
            map[X, Y] = Tile.Snake;
            PositionFood(map, width, height);
            Console.SetCursorPosition(X, Y);
            Console.Write('@');

            while (!direction.HasValue && !closeRequested)
            {
                GetDirection(ref direction, ref closeRequested);
            }

            while (!closeRequested)
            {
                if (Console.WindowWidth != width || Console.WindowHeight != height)
                {
                    Console.Clear();
                    Console.Write("Console was resized. Snake game has ended.");
                    return;
                }

                switch (direction)
                {
                    case Direction.Up: Y--; break;
                    case Direction.Down: Y++; break;
                    case Direction.Left: X--; break;
                    case Direction.Right: X++; break;
                }

                if (X < 0 || X >= width ||
                    Y < 0 || Y >= height ||
                    map[X, Y] is Tile.Snake)
                {
                    Console.Clear();
                    Console.Write("Game Over. Score: " + (snake.Count - 1) + ".");
                    return;
                }

                Console.SetCursorPosition(X, Y);
                Console.Write(DirectionChars[(int)direction!]);
                snake.Enqueue((X, Y));

                if (map[X, Y] is Tile.Food)
                {
                    PositionFood(map, width, height);
                }
                else
                {
                    (int x, int y) = snake.Dequeue();
                    map[x, y] = Tile.Open;
                    Console.SetCursorPosition(x, y);
                    Console.Write(' ');
                }

                map[X, Y] = Tile.Snake;

                if (Console.KeyAvailable)
                {
                    GetDirection(ref direction, ref closeRequested);
                }

                System.Threading.Thread.Sleep(sleep);
            }
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            Console.CursorVisible = true;
            Console.Clear();
            Console.WriteLine(exception?.ToString() ?? "Snake was closed.");
        }
    }

}
