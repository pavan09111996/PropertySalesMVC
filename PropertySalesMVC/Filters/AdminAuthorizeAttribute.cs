using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertySalesMVC.Helpers;

namespace PropertySalesMVC.Filters
{
    /// <summary>
    /// Requires an active Admin-role session. A logged-in Developer session
    /// is deliberately NOT let through here — Error Logs is the only page
    /// they're allowed to see (see DeveloperAuthorizeAttribute) — so they're
    /// redirected to their own home page rather than the public site.
    /// </summary>
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var role = session.GetString(SessionKeys.Role);

            if (role == Roles.Admin)
            {
                base.OnActionExecuting(context);
                return;
            }

            context.Result = role == Roles.Developer
                ? new RedirectToActionResult("ErrorLogs", "Admin", null)
                : new RedirectToActionResult("Index", "Home", null);
        }
    }
}
