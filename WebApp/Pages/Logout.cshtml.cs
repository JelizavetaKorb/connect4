using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Pages;

public class LogoutModel : PageModel
{
    public IActionResult OnGet()
    {
        HttpContext.Session.ClearCurrentUser();
        return RedirectToPage("/Index");
    }

    public IActionResult OnPost()
    {
        HttpContext.Session.ClearCurrentUser();
        return RedirectToPage("/Index");
    }
}