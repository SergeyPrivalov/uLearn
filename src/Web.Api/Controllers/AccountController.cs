using Microsoft.AspNetCore.Mvc;
using Serilog;
using Ulearn.Common.Extensions;

namespace Web.Api.Controllers
{
	[Route("/account")]
	public class AccountController : BaseController
	{
		public AccountController(ILogger logger)
			: base(logger)
		{
		}

		[HttpGet]
		public IActionResult Me()
		{
			return Json(new {
				userId = User.GetUserId()
			});
		}
	}
}