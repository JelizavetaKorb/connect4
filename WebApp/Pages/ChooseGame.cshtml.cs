using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Pages;

public class ChooseGameModel : PageModel
{
    public string? Username { get; set; }

    public IActionResult OnGet()
    {
        Username = HttpContext.Session.GetCurrentUsername();
    
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
        HttpContext.Session.Remove("GameBrain");
        HttpContext.Session.Remove("CurrentGameId");
        HttpContext.Session.Remove("GameName");
        return Page();
    }
}