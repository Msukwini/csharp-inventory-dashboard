using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace inventory_dashboard.Controllers;

public class BaseController : Controller
{
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext filterContext)
    {
        // Skip login check for LoginController
        var controller = filterContext.Controller as Controller;
        if (controller is LoginController)
        {
            base.OnActionExecuting(filterContext);
            return;
        }

        // Check if user is logged in
        var username = HttpContext.Session.GetString("Username");
        if (string.IsNullOrEmpty(username))
        {
            filterContext.Result = RedirectToAction("Index", "Login");
            return;
        }

        base.OnActionExecuting(filterContext);
    }
}
