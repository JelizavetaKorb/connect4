using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp.Services;

namespace WebApp.Pages;

public class AccountModel : PageModel
{
    public string? Username { get; set; }

    public IActionResult OnGet()
    {
        Username = HttpContext.Session.GetCurrentUsername();
        
        if (string.IsNullOrEmpty(Username))
        {
            return RedirectToPage("/Index");
        }
        return Page();
    }
    
    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Index");
    }
}