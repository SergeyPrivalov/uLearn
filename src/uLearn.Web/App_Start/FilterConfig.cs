using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using Database.DataContexts;
using Microsoft.AspNet.Identity;
using uLearn.Extensions;

namespace uLearn.Web
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
			var requireHttps = Convert.ToBoolean(WebConfigurationManager.AppSettings["ulearn.requireHttps"] ?? "true");
			if (requireHttps)
				filters.Add(new RequireHttpsForCloudFlareAttribute());
			filters.Add(new AntiForgeryTokenFilter());
			filters.Add(new KonturPassportRequiredFilter());
		}
	}

	public class AntiForgeryTokenFilter : FilterAttribute, IExceptionFilter
	{
		public void OnException(ExceptionContext filterContext)
		{
			if (!(filterContext.Exception is HttpAntiForgeryException))
				return;

			if (filterContext.RequestContext.HttpContext.Request.IsAjaxRequest())
				filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden);
			else
				filterContext.Result = new RedirectResult("/");

			filterContext.ExceptionHandled = true;
		}
	}

	public static class HttpRequestExtensions
	{
		private static readonly string xSchemeHeaderName = "X-Scheme";

		/* Return scheme from request of from header X-Scheme if request has been proxied by cloudflare or nginx or ... */
		public static string GetRealScheme(this HttpRequestBase request)
		{
			return request.Headers[xSchemeHeaderName] ?? request.Url?.Scheme;
		}
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class RequireHttpsForCloudFlareAttribute : RequireHttpsAttribute
	{
		/* Additionally view real scheme from headers. If it equals to "HTTPS", continue work */
		protected override void HandleNonHttpsRequest(AuthorizationContext filterContext)
		{
			if (string.Equals(filterContext.HttpContext.Request.GetRealScheme(), "HTTPS", StringComparison.OrdinalIgnoreCase))
				return;
			if (!string.Equals(filterContext.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
				!string.Equals(filterContext.HttpContext.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Require HTTPS");
			var url = "https://" + filterContext.HttpContext.Request.Url?.Host + filterContext.HttpContext.Request.RawUrl;
			filterContext.Result = new RedirectResult(url);
		}
	}

	public class KonturPassportRequiredFilter : ActionFilterAttribute
	{
		/* If query string contains &konturPassport=true then we need to check kontur.passport login */
		private const string queryStringParameterName = "kontur";

		private readonly ULearnUserManager userManager;

		public KonturPassportRequiredFilter(ULearnUserManager userManager)
		{
			this.userManager = userManager;
		}

		public KonturPassportRequiredFilter()
			: this(new ULearnUserManager(new ULearnDb()))
		{
		}

		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var httpContext = filterContext.RequestContext.HttpContext;

			var originalUrl = httpContext.Request.Url?.ToString().RemoveQueryParameter(queryStringParameterName) ?? "";

			var isAuthenticated = httpContext.User.Identity.IsAuthenticated;
			if (isAuthenticated)
			{
			}
			else
			{
				filterContext.Result = RedirectToAction("EnsureKonturProfileLogin", "Login", new
				{
					returnUrl = originalUrl
				});
			}
		}

		private ActionResult RedirectToAction(string actionName, string controllerName, RouteValueDictionary values)
		{
			if (string.IsNullOrEmpty(actionName))
				throw new ArgumentNullException(nameof(actionName));

			if (string.IsNullOrEmpty(controllerName))
				throw new ArgumentNullException(nameof(controllerName));

			values.Add("action", actionName);
			values.Add("controller", controllerName);
			return new RedirectToRouteResult(values);
		}

		private ActionResult RedirectToAction(string actionName, string controllerName, Dictionary<string, string> values)
		{
			var routeValues = new RouteValueDictionary();
			foreach (var kpv in values)
				routeValues.Add(kpv.Key, kpv.Value);
			return RedirectToAction(actionName, controllerName, routeValues);
		}

		private ActionResult RedirectToAction(string actionName, string controllerName, object values)
		{
			return RedirectToAction(actionName, controllerName, HtmlHelper.AnonymousObjectToHtmlAttributes(values));
		}

		private ActionResult RedirectToAction(string actionName, string controllerName)
		{
			return RedirectToAction(actionName, controllerName, new Dictionary<string, string>());
		}
	}
}