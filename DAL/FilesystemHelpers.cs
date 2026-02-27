namespace DAL;

public static class FilesystemHelpers
{
    private const string AppName = "Connect4";

    // used for JSON files location
    public static string GetGameDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var finalDirectory = homeDirectory + Path.DirectorySeparatorChar + AppName + Path.DirectorySeparatorChar +
                             "savegames";

        Directory.CreateDirectory(finalDirectory);
        return finalDirectory;
    }
}