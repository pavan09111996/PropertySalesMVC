using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertySalesMVC.Helpers;

namespace PropertySalesMVC.Filters
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            var isLoggedIn = session.GetString(SessionKeys.IsAdminLoggedIn);

            if (string.IsNullOrEmpty(isLoggedIn))
            {
                context.Result = new RedirectToActionResult(
                    "Index", "Home", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
