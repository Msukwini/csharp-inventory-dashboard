using Microsoft.AspNetCore.Mvc;
using inventory_dashboard.Services;

namespace inventory_dashboard.Controllers;

public class HomeController : BaseController
{
    private readonly DashboardService _dashboardService;

    public HomeController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var data = await _dashboardService.GetDashboardDataAsync();
        return View(data);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
