using System;
using System.Linq;
using Database.Repos;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Vostok.Tracing;

namespace Web.Api.Controllers
{
    [Route("/home")]
    public class HomeController : Controller
    {
		private readonly UsersRepo usersRepo;
		private readonly CommentsRepo commentsRepo;

		public HomeController(UsersRepo usersRepo, CommentsRepo commentsRepo)
		{
			this.usersRepo = usersRepo;
			this.commentsRepo = commentsRepo;
		}
		
        [HttpGet("{*url}")]
        public object Echo()
		{
			var comment = commentsRepo.FindCommentById(10);
			var user = usersRepo.FindUsersByUsernameOrEmail("user").First();
            return Json(new
            {
				userName = user.VisibleName,
				commentText = comment?.Text,
                url = Request.GetUri(),
                traceUrl = $"http://localhost:6301/{TraceContext.Current.TraceId}",
                traceId = TraceContext.Current.TraceId
            });
        }
    }
}
