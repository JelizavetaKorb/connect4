namespace WebApp.Services;
public static class SessionExtensions
{
    private const string UsernameKey = "CurrentUsername";

    public static void SetCurrentUser(this ISession session, string username)
    {
        session.SetString(UsernameKey, username);
    }

    public static string? GetCurrentUsername(this ISession session)
    {
        return session.GetString(UsernameKey);
    }

    public static void ClearCurrentUser(this ISession session)
    {
        session.Remove(UsernameKey);
        session.Remove("GameBrain");
    }
}