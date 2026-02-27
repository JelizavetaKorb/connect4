using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DAL;
using BLL;
using WebApp.Services;

namespace WebApp.Pages;

public class SavedGamesModel : PageModel
{
    private readonly IRepository<GameState> _activeRepo;
    private readonly IRepository<GameState> _otherRepo;
    private readonly MultiplayerSessionManager _sessionManager;

    public SavedGamesModel(RepositoryProvider repoProvider, MultiplayerSessionManager sessionManager)
    {
        _activeRepo = repoProvider.ActiveRepo;
        _otherRepo = repoProvider.OtherRepo;
        _sessionManager = sessionManager;
    }

    public string? Username { get; set; }
    public List<(string id, string description)> SavedGames { get; set; } = new();

    // loads and displays all saved games for the current user with descriptions
    public IActionResult OnGet()
    {
        Username = HttpContext.Session.GetCurrentUsername();
    
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
    
        var allGames = _activeRepo.List(Username);
        SavedGames = new List<(string id, string description)>();
    
        foreach (var (id, description) in allGames)
        {
            try
            {
                var game = _activeRepo.Load(id);
                if (game != null)
                {
                    bool isOnlineMultiplayer = _sessionManager.IsMultiplayerGame(id) || 
                                               game.Name.Contains(" vs ", StringComparison.OrdinalIgnoreCase);
                
                    var opponentName = game.Configuration.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase)
                        ? game.Configuration.Player2Name
                        : game.Configuration.Player1Name;
                    
                    if (isOnlineMultiplayer)
                    {
                        SavedGames.Add((id, $"Multiplayer game with {opponentName}"));
                    }
                    else
                    {
                        SavedGames.Add((id, $"{game.Name} with {opponentName}"));
                    }
                }
                else
                {
                    SavedGames.Add((id, description));
                }
            }
            catch
            {
                SavedGames.Add((id, description));
            }
        }
        return Page();
    }

    // loads a saved game, recreates multiplayer session if needed, sends player to game page
    public IActionResult OnPostLoadGame(string gameId)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }

        GameState? savedGame = null;

        try
        {
            savedGame = _activeRepo.Load(gameId);
        }
        catch
        {
            TempData["ErrorMessage"] = "This game has been deleted by another player.";
            return RedirectToPage();
        }

        if (savedGame == null)
        {
            TempData["ErrorMessage"] = "This game has been deleted by another player.";
            return RedirectToPage();
        }

        if (!savedGame.Configuration.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase) &&
            !savedGame.Configuration.Player2Name.Equals(Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "You are not a player in this game!";
            return RedirectToPage();
        }

        bool isMultiplayer = _sessionManager.IsMultiplayerGame(gameId);

        if (isMultiplayer)
        {
            if (!_sessionManager.IsMultiplayerGame(gameId))
            {
                var player1Name = savedGame.Configuration.Player1Name;
                var player2Name = savedGame.Configuration.Player2Name;
                var joinCode = _sessionManager.CreateSession(gameId, player1Name);

                _sessionManager.JoinSession(joinCode, player2Name);
            }

            _sessionManager.UpdateActivity(gameId, Username);
            _sessionManager.SetPlayerWantsToExit(gameId, savedGame.Configuration.Player1Name, false);
            _sessionManager.SetPlayerWantsToExit(gameId, savedGame.Configuration.Player2Name, false);
            _sessionManager.ResetPlayerLeftFlags(gameId);
        }

        HttpContext.Session.SetString("CurrentGameId", savedGame.Id.ToString());
        HttpContext.Session.SetString("GameName", savedGame.Name);
        if (!isMultiplayer)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(savedGame);
            HttpContext.Session.SetString("GameBrain", json);
        }

        return RedirectToPage("/Game");
    }

    // validates join code, adds player 2 to the game, saves updated game, redirects to game page
    public IActionResult OnPostJoinGame(string joinCode)
    {
        Username = HttpContext.Session.GetCurrentUsername();

        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrWhiteSpace(joinCode) || joinCode.Length != 6)
        {
            TempData["ErrorMessage"] = "Invalid join code format!";
            return RedirectToPage("/ChooseGame");
        }

        var session = _sessionManager.GetSessionByJoinCode(joinCode);

        if (session == null)
        {
            TempData["ErrorMessage"] = "Invalid join code!";
            return RedirectToPage("/ChooseGame");
        }

        if (session.Player2Name != null && !session.Player2Name.StartsWith("[Waiting"))
        {
            TempData["ErrorMessage"] = "Game already has two players!";
            return RedirectToPage("/ChooseGame");
        }

        if (session.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "You cannot join your own game!";
            return RedirectToPage("/ChooseGame");
        }
        var savedGame = _activeRepo.Load(session.GameId);

        if (savedGame == null)
        {
            TempData["ErrorMessage"] = "Game not found!";
            return RedirectToPage("/ChooseGame");
        }
        savedGame.Configuration.Player2Name = Username;
        if (_sessionManager.JoinSession(joinCode, Username))
        {
            _activeRepo.Save(savedGame);
            _otherRepo.Save(savedGame);
            HttpContext.Session.SetString("CurrentGameId", savedGame.Id.ToString());
            HttpContext.Session.SetString("GameName", savedGame.Name);
            TempData["SaveMessage"] = "Successfully joined multiplayer game!";
            return RedirectToPage("/Game");
        }

        TempData["ErrorMessage"] = "Failed to join game. Please try again.";
        return RedirectToPage("/ChooseGame");
    }

    // checks if user is a player in the game, then deletes it from both repositories and session manager
    public IActionResult OnPostDeleteGame(string gameId)
    {
        Username = HttpContext.Session.GetCurrentUsername();
        
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
        try
        {
            var savedGame = _activeRepo.Load(gameId);
            
            if (!savedGame.Configuration.Player1Name.Equals(Username, StringComparison.OrdinalIgnoreCase) &&
                !savedGame.Configuration.Player2Name.Equals(Username, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "You cannot delete a game you're not a player in!";
                return RedirectToPage();
            }
        }
        catch
        {
            TempData["ErrorMessage"] = "Game not found!";
            return RedirectToPage();
        }

        _activeRepo.Delete(gameId);
        _otherRepo.Delete(gameId);
        _sessionManager.RemoveSession(gameId);
        
        TempData["SaveMessage"] = "Game deleted successfully!";
        return RedirectToPage();
    }
}
