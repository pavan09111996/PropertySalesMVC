using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertySalesMVC.Helpers;

namespace PropertySalesMVC.Filters
{
    /// <summary>
    /// Requires an active Developer-role session. Used only on Error Logs —
    /// a logged-in Admin session is deliberately NOT let through here, since
    /// Error Logs is meant to be developer-only, not visible to Admin.
    /// </summary>
    public class DeveloperAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            var role = session.GetString(SessionKeys.Role);

            if (role == Roles.Developer)
            {
                base.OnActionExecuting(context);
                return;
            }

            context.Result = role == Roles.Admin
                ? new RedirectToActionResult("Dashboard", "Admin", null)
                : new RedirectToActionResult("Index", "Home", null);
        }
    }
}
