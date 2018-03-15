using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Web.Api.Controllers
{
	public class BaseController : Controller
	{
		protected readonly ILogger logger;

		public BaseController(ILogger logger)
		{
			this.logger = logger;
		}
	}
}