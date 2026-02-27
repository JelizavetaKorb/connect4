using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Pages;

public class IndexModel : PageModel
{
    public string? CurrentUser { get; set; }

    public void OnGet()
    {
        CurrentUser = HttpContext.Session.GetCurrentUsername();
        
        if (!string.IsNullOrEmpty(CurrentUser))
        {
            Response.Redirect("/ChooseGame");
        }
    }

    public IActionResult OnPostSetName(string username)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            HttpContext.Session.SetCurrentUser(username);
        }
        return RedirectToPage("/ChooseGame");
    }
}