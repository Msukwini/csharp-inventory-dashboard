using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace inventory_dashboard.Controllers;

public class LoginController : Controller
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin123!";

    // GET: /Login
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("Username") != null)
        {
            return RedirectToAction("Index", "Home");
        }
        return View("Login"); // Explicitly uses Login.cshtml
    }

    // POST: /Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string username, string password)
    {
        if (username == AdminUsername && password == AdminPassword)
        {
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("IsAdmin", "true");
            return RedirectToAction("Index", "Home");
        }

        ViewBag.Error = "Invalid username or password.";
        return View("Login");
    }

    // GET: /Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Login");
    }
}
