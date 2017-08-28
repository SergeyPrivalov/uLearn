using System;
using System.Web.Configuration;
using Database.DataContexts;
using Database.Extensions;
using Database.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using uLearn.Web.LTI;
using uLearn.Web.Microsoft.Owin.Security.VK;

namespace uLearn.Web
{
	public partial class Startup
	{
		// For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
		public void ConfigureAuth(IAppBuilder app)
		{
			app.CreatePerOwinContext(() => new ULearnUserManager(new ULearnDb()));

			// Enable the application to use a cookie to store information for the signed in user
			app.UseCookieAuthentication(new CookieAuthenticationOptions
			{
				AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
				LoginPath = new PathString("/Login"),
				Provider = new CookieAuthenticationProvider
				{
					OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<UserManager<ApplicationUser>, ApplicationUser, String>(
						validateInterval: TimeSpan.FromMinutes(30),
						regenerateIdentityCallback: (manager, user) => user.GenerateUserIdentityAsync(manager),
						getUserIdCallback: identity => identity.GetUserId()
					)
				}
			});
			// Use a cookie to temporarily store information about a user logging in with a third party login provider
			app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

			// Uncomment the following lines to enable logging in with third party login providers
			//app.UseMicrosoftAccountAuthentication(
			//	clientId: "",
			//	clientSecret: "");

			//app.UseTwitterAuthentication(
			//	consumerKey: "hC6XpJy0OPVkbvGzRIOJRA",
			//	  consumerSecret: "cEDewTtU7RKHimj2D1IpD75HUKnjVeobdSNhjAAQ");

			var vkAppId = WebConfigurationManager.AppSettings["owin.vk.appId"];
			var vkAppSecret = WebConfigurationManager.AppSettings["owin.vk.appSecret"];
			app.UseVkAuthentication(vkAppId, vkAppSecret);
			//app.UseFacebookAuthentication(
			//   appId: "",
			//   appSecret: "");

			//app.UseGoogleAuthentication();
			app.UseLtiAuthentication();
		}
	}
}