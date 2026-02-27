using WebApp.Models;

namespace WebApp.Services;

public class MultiplayerSessionManager
{
    private readonly Dictionary<string, MultiplayerSession> _sessions = new();
    private readonly Dictionary<string, string> _joinCodeToGameId = new();
    private readonly object _lock = new object();

    public string CreateSession(string gameId, string player1Name)
    {
        lock (_lock)
        {
            var joinCode = GenerateJoinCode();
            var session = new MultiplayerSession
            {
                GameId = gameId,
                JoinCode = joinCode,
                Player1Name = player1Name,
                LastActivityPlayer1 = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _sessions[gameId] = session;
            _joinCodeToGameId[joinCode] = gameId;
            return joinCode;
        }
    }

    public bool JoinSession(string joinCode, string player2Name)
    {
        lock (_lock)
        {
            if (_joinCodeToGameId.TryGetValue(joinCode, out var gameId))
            {
                if (_sessions.TryGetValue(gameId, out var session) && session.Player2Name == null)
                {
                    session.Player2Name = player2Name;
                    session.LastActivityPlayer2 = DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }
    }

    public MultiplayerSession? GetSession(string gameId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(gameId, out var session) ? session : null;
        }
    }

    public MultiplayerSession? GetSessionByJoinCode(string joinCode)
    {
        lock (_lock)
        {
            if (_joinCodeToGameId.TryGetValue(joinCode, out var gameId))
            {
                return GetSession(gameId);
            }
            return null;
        }
    }

    public void UpdateActivity(string gameId, string playerName)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                if (session.Player1Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.LastActivityPlayer1 = DateTime.UtcNow;
                else if (session.Player2Name != null && session.Player2Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.LastActivityPlayer2 = DateTime.UtcNow;
            }
        }
    }

    public bool IsOpponentActive(string gameId, string myName)
    {
        lock (_lock)
        {
            // for demo purposes, always return true 
            return _sessions.ContainsKey(gameId);
        }
    }

    public void SetPlayerWantsToExit(string gameId, string playerName, bool wantsToExit)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                if (session.Player1Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.Player1WantsToExit = wantsToExit;
                else if (session.Player2Name != null && session.Player2Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.Player2WantsToExit = wantsToExit;
            }
        }
    }

    public bool DoesOpponentWantToExit(string gameId, string myName)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                var isPlayer1 = session.Player1Name.Equals(myName, StringComparison.OrdinalIgnoreCase);
                return isPlayer1 ? session.Player2WantsToExit : session.Player1WantsToExit;
            }
            return false;
        }
    }

    public void RemoveSession(string gameId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                _joinCodeToGameId.Remove(session.JoinCode);
                _sessions.Remove(gameId);
            }
        }
    }

    public bool IsMultiplayerGame(string gameId)
    {
        lock (_lock)
        {
            return _sessions.ContainsKey(gameId);
        }
    }

    private string GenerateJoinCode()
    {
        var random = new Random();
        string code;
        do
        {
            code = random.Next(100000, 999999).ToString();
        } while (_joinCodeToGameId.ContainsKey(code));
        
        return code;
    }
    
    public void MarkPlayerLeft(string gameId, string playerName)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                if (session.Player1Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.Player1HasLeft = true;
                else if (session.Player2Name != null && session.Player2Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    session.Player2HasLeft = true;
                if (session.Player1HasLeft && session.Player2HasLeft)
                {
                    RemoveSession(gameId);
                }
            }
        }
    }

    public void ResetPlayerLeftFlags(string gameId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                session.Player1HasLeft = false;
                session.Player2HasLeft = false;
                session.Player1WantsToExit = false;
                session.Player2WantsToExit = false;
            }
        }
    }
    
    public bool BothPlayersHaveLeft(string gameId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(gameId, out var session))
            {
                return session.Player1HasLeft && session.Player2HasLeft;
            }
            return false;
        }
    }
}