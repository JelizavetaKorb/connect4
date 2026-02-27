namespace DAL;

public static class DataDirectoryConfig
{
    // used for db storage
    public static string? DataDirectory { get; set; }
    public static bool UseJsonRepository { get; set; } = true;
}