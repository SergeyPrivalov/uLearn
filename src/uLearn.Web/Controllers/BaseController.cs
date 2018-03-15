using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;

namespace uLearn.Web.Controllers
{
	public class BaseController : Controller
	{
		private readonly List<string> utmTags = new List<string> { "utm_source", "utm_medium", "utm_term", "utm_content", "utm_name" }; 
		
		protected override RedirectToRouteResult RedirectToAction(string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			var redirectResult = base.RedirectToAction(actionName, controllerName, routeValues);
			return AddUtmTagsInRedirectResult(redirectResult);
		}

		protected override RedirectToRouteResult RedirectToActionPermanent(string actionName, string controllerName, RouteValueDictionary routeValues)
		{
			var redirectResult = base.RedirectToActionPermanent(actionName, controllerName, routeValues);
			return AddUtmTagsInRedirectResult(redirectResult);
		}
		
		protected override RedirectToRouteResult RedirectToRoute(string routeName, RouteValueDictionary routeValues)
		{
			var redirectResult = base.RedirectToRoute(routeName, routeValues);
			return AddUtmTagsInRedirectResult(redirectResult);
		}

		protected override RedirectToRouteResult RedirectToRoutePermanent(string routeName, RouteValueDictionary routeValues)
		{
			var redirectResult = base.RedirectToRoutePermanent(routeName, routeValues);
			return AddUtmTagsInRedirectResult(redirectResult);
		}

		private RedirectToRouteResult AddUtmTagsInRedirectResult(RedirectToRouteResult redirectResult)
		{
			foreach (var utmTag in utmTags)
			{
				var utmTagValue = HttpContext.Request.QueryString.Get(utmTag);
				if (!string.IsNullOrEmpty(utmTagValue))
					redirectResult.RouteValues.Add(utmTag, utmTagValue);
			}

			return redirectResult;
		}
	}
}