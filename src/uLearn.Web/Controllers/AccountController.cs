﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using uLearn.Web.DataContexts;
using uLearn.Web.Models;

namespace uLearn.Web.Controllers
{
	[Authorize]
	public class AccountController : Controller
	{
		private readonly ULearnDb db;
		private readonly CourseManager courseManager;
		public AccountController()
			: this(new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ULearnDb())))
		{
			db = new ULearnDb();
			courseManager = WebCourseManager.Instance;
			UserManager.UserValidator =
				new UserValidator<ApplicationUser>(UserManager)
				{
					AllowOnlyAlphanumericUserNames = false
				};
		}

		public AccountController(UserManager<ApplicationUser> userManager)
		{
			UserManager = userManager;
		}

		public UserManager<ApplicationUser> UserManager { get; private set; }

		[Authorize(Roles = LmsRoles.Admin + "," + LmsRoles.Instructor)]
		public ActionResult List(string namePrefix = null, string role = null)
		{
			IQueryable<ApplicationUser> applicationUsers = new ULearnDb().Users;
			if (!string.IsNullOrEmpty(namePrefix)) applicationUsers = applicationUsers.Where(u => u.UserName.StartsWith(namePrefix));
			if (!string.IsNullOrEmpty(role)) applicationUsers = applicationUsers.Where(u => u.Roles.Any(r => r.Role.Name == role));
			return View(applicationUsers.OrderBy(u => u.UserName).Take(50).ToList());
		}

		[Authorize(Roles = LmsRoles.Admin)]
		public async Task<ActionResult> ToggleRole(string userId, string role)
		{
			if (UserManager.IsInRole(userId, role))
				await UserManager.RemoveFromRoleAsync(userId, role);
			else
				await UserManager.AddToRolesAsync(userId, role);
			return RedirectToAction("List");
		}

		[HttpPost]
		[Authorize(Roles = LmsRoles.Admin)]
		public async Task<ActionResult> DeleteUser(string userId)
		{
			ApplicationUser user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
			if (user != null)
			{
				db.Users.Remove(user);
				await db.SaveChangesAsync();
			}
			return RedirectToAction("List");
		}

		[Authorize(Roles = LmsRoles.Admin + "," + LmsRoles.Instructor)]
		public ActionResult Info(string userName)
		{
			var user = db.Users.FirstOrDefault(u => u.Id == userName || u.UserName == userName);
			if (user == null) return RedirectToAction("List");
			var courses = new HashSet<string>(db.Visiters.Where(v => v.UserId == user.Id).Select(v => v.CourseId).Distinct());
			return View(new UserInfoModel(user, courseManager.GetCourses().Where(c => courses.Contains(c.Id)).ToArray()));
		}

		[Authorize(Roles = LmsRoles.Admin + "," + LmsRoles.Instructor)]
		public ActionResult CourseInfo(string userName, string courseId)
		{
			var user = db.Users.FirstOrDefault(u => u.Id == userName || u.UserName == userName);
			if (user == null) return RedirectToAction("List");
			var course = courseManager.GetCourse(courseId);
			return View(new UserCourseModel(course, user, db));
		}

		//
		// GET: /Account/Login
		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		protected override void OnException(ExceptionContext filterContext)
		{
			if (filterContext.Exception is HttpAntiForgeryException)
			{
				filterContext.ExceptionHandled = true;
				filterContext.Result = RedirectToAction("Login");
			}
			base.OnException(filterContext);
		}

		//
		// POST: /Account/Login
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
		{
			if (ModelState.IsValid)
			{
				var user = await UserManager.FindAsync(model.UserName, model.Password);
				if (user != null)
				{
					await SignInAsync(user, model.RememberMe);
					return RedirectToLocal(returnUrl);
				}
				else
				{
					ModelState.AddModelError("", "Неверное имя пользователя или пароль.");
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// GET: /Account/Register
		[AllowAnonymous]
		public ActionResult Register(string returnUrl = null)
		{
			return View(new RegisterViewModel { ReturnUrl = returnUrl });
		}

		//
		// POST: /Account/Register
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Register(RegisterViewModel model)
		{
			if (ModelState.IsValid)
			{
				var user = new ApplicationUser() { UserName = model.UserName };
				var result = await UserManager.CreateAsync(user, model.Password);
				if (result.Succeeded)
				{
					await SignInAsync(user, isPersistent: false);
					if (string.IsNullOrWhiteSpace(model.ReturnUrl))
						return RedirectToAction("Index", "Home");
					return RedirectToLocal(model.ReturnUrl);
				}
				AddErrors(result);
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/Disassociate
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
		{
			ManageMessageId? message = null;
			IdentityResult result =
				await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
			if (result.Succeeded)
			{
				message = ManageMessageId.RemoveLoginSuccess;
			}
			else
			{
				message = ManageMessageId.Error;
			}
			return RedirectToAction("Manage", new { Message = message });
		}

		//
		// GET: /Account/Manage
		public ActionResult Manage(ManageMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess
					? "Пароль был изменен."
					: message == ManageMessageId.SetPasswordSuccess
						? "Пароль установлен."
						: message == ManageMessageId.RemoveLoginSuccess
							? "Внешний логин удален."
							: message == ManageMessageId.Error
								? "Ошибка."
								: "";
			ViewBag.HasLocalPassword = HasPassword();
			ViewBag.ReturnUrl = Url.Action("Manage");
			return View();
		}

		//
		// POST: /Account/Manage
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> Manage(ManageUserViewModel model)
		{
			bool hasPassword = HasPassword();
			ViewBag.HasLocalPassword = hasPassword;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasPassword)
			{
				if (ModelState.IsValid)
				{
					IdentityResult result =
						await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
					if (result.Succeeded)
					{
						return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
					}
					else
					{
						AddErrors(result);
					}
				}
			}
			else
			{
				// User does not have a password so remove any validation errors caused by a missing OldPassword field
				ModelState state = ModelState["OldPassword"];
				if (state != null)
				{
					state.Errors.Clear();
				}

				if (ModelState.IsValid)
				{
					IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
					if (result.Succeeded)
					{
						return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
					}
					else
					{
						AddErrors(result);
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/ExternalLogin
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogin(string provider, string returnUrl)
		{
			// Request a redirect to the external login provider
			return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
		}

		//
		// GET: /Account/ExternalLoginCallback
		[AllowAnonymous]
		public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
		{
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
			if (loginInfo == null)
			{
				return RedirectToAction("Login");
			}

			// Sign in the user with this external login provider if the user already has a login
			var user = await UserManager.FindAsync(loginInfo.Login);
			if (user != null)
			{
				await SignInAsync(user, isPersistent: false);
				return RedirectToLocal(returnUrl);
			}
			else
			{
				// If the user does not have an account, then prompt the user to create an account
				ViewBag.ReturnUrl = returnUrl;
				ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
				return View("ExternalLoginConfirmation",
					new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
			}
		}

		//
		// POST: /Account/LinkLogin
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LinkLogin(string provider)
		{
			// Request a redirect to the external login provider to link a login for the current user
			return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
		}

		//
		// GET: /Account/LinkLoginCallback
		public async Task<ActionResult> LinkLoginCallback()
		{
			var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
			if (loginInfo == null)
			{
				return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
			}
			var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
			if (result.Succeeded)
			{
				return RedirectToAction("Manage");
			}
			return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
		}

		//
		// POST: /Account/ExternalLoginConfirmation
		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("Manage");
			}

			if (ModelState.IsValid)
			{
				// Get the information about the user from the external login provider
				var info = await AuthenticationManager.GetExternalLoginInfoAsync();
				if (info == null)
				{
					return View("ExternalLoginFailure");
				}
				var user = new ApplicationUser() { UserName = model.UserName };
				var result = await UserManager.CreateAsync(user);
				if (result.Succeeded)
				{
					result = await UserManager.AddLoginAsync(user.Id, info.Login);
					if (result.Succeeded)
					{
						await SignInAsync(user, isPersistent: false);
						return RedirectToLocal(returnUrl);
					}
				}
				AddErrors(result);
			}

			ViewBag.ReturnUrl = returnUrl;
			return View(model);
		}

		//
		// POST: /Account/LogOff
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff()
		{
			AuthenticationManager.SignOut();
			return RedirectToAction("Index", "Home");
		}

		//
		// GET: /Account/ExternalLoginFailure
		[AllowAnonymous]
		public ActionResult ExternalLoginFailure()
		{
			return View();
		}

		[ChildActionOnly]
		public ActionResult RemoveAccountList()
		{
			var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
			ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
			return (ActionResult)PartialView("_RemoveAccountPartial", linkedAccounts);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && UserManager != null)
			{
				UserManager.Dispose();
				UserManager = null;
			}
			base.Dispose(disposing);
		}

		#region Helpers

		// Used for XSRF protection when adding external logins
		private const string XsrfKey = "XsrfId";

		private IAuthenticationManager AuthenticationManager
		{
			get { return HttpContext.GetOwinContext().Authentication; }
		}

		private async Task SignInAsync(ApplicationUser user, bool isPersistent)
		{
			AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
			var identity = await UserManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
			AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
		}

		private void AddErrors(IdentityResult result)
		{
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError("", error);
			}
		}

		private bool HasPassword()
		{
			var user = UserManager.FindById(User.Identity.GetUserId());
			if (user != null)
			{
				return user.PasswordHash != null;
			}
			return false;
		}

		public enum ManageMessageId
		{
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			Error
		}

		private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			else
			{
				return RedirectToAction("Index", "Home");
			}
		}

		private class ChallengeResult : HttpUnauthorizedResult
		{
			public ChallengeResult(string provider, string redirectUri)
				: this(provider, redirectUri, null)
			{
			}

			public ChallengeResult(string provider, string redirectUri, string userId)
			{
				LoginProvider = provider;
				RedirectUri = redirectUri;
				UserId = userId;
			}

			public string LoginProvider { get; set; }
			public string RedirectUri { get; set; }
			public string UserId { get; set; }

			public override void ExecuteResult(ControllerContext context)
			{
				var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
				if (UserId != null)
				{
					properties.Dictionary[XsrfKey] = UserId;
				}
				context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
			}
		}

		#endregion

		[Authorize]
		public async Task<PartialViewResult> ChangeDetailsPartial()
		{
			var user = await UserManager.FindByNameAsync(User.Identity.Name);
			return PartialView(new UserViewModel { Name = user.UserName, GroupName = user.GroupName, UserId = user.Id });
		}

		[HttpPost]
		public async Task<ActionResult> ChangeDetailsPartial(UserViewModel userModel)
		{
			var user = await UserManager.FindByIdAsync(userModel.UserId);
			if (user == null)
			{
				AuthenticationManager.SignOut();
				return RedirectToAction("Login");
			}
			var nameChanged = user.UserName != userModel.Name;
			if (nameChanged && await UserManager.FindByNameAsync(userModel.Name) != null)
				return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
			user.UserName = userModel.Name;
			user.GroupName = userModel.GroupName;
			await UserManager.UpdateAsync(user);

			if (nameChanged)
			{
				AuthenticationManager.SignOut();
				return RedirectToAction("Login");
			}
			return RedirectToAction("Manage");
		}

		[HttpPost, Authorize(Roles = LmsRoles.Admin)]
		public async Task<ActionResult> ResetPassword(string newPassword, string userId)
		{
			var user = await UserManager.FindByIdAsync(userId);
			if (user == null) return RedirectToAction("List");
			await UserManager.RemovePasswordAsync(userId);
			await UserManager.AddPasswordAsync(userId, newPassword);
			return RedirectToAction("Info", new{user.UserName});
		}
	}

	public class UserInfoModel
	{
		public ApplicationUser User { get; set; }
		public Course[] Courses { get; private set; }

		public UserInfoModel(ApplicationUser user, Course[] courses)
		{
			User = user;
			Courses = courses;
		}
	}

	public class UserCourseModel
	{

		public UserCourseModel(Course course, ApplicationUser user, ULearnDb db)
		{
			Course = course;
			User = user;
			var visits = db.Visiters.Where(v => v.UserId == user.Id && v.CourseId == course.Id).ToList();
			var quizes = db.UserQuizzes.Where(v => v.UserId == user.Id && v.CourseId == course.Id && !v.isDropped).ToList();
			var exercises = db.UserSolutions.Where(v => v.UserId == user.Id && v.CourseId == course.Id && v.IsRightAnswer && !v.IsCompilationError).ToList();
			Units = course.GetUnits().Select(unitName =>
				new UserCourseUnitModel
				{
					UnitName = unitName,
					SlideVisits = CreateProgress(course, unitName, visits, v => v.SlideId, s => 1),
					Exercises = CreateProgress(course, unitName, exercises, e => e.SlideId, s => s is ExerciseSlide ? 1 : 0),
					Quizes = CreateProgress(course, unitName, quizes, q => q.SlideId, s => s is QuizSlide ? 1 : 0),
				}
				).ToArray();
		}

		public ProgressModel CreateProgress<T>(Course course, string unitName, List<T> items, Func<T, string> getSlideId, Func<Slide, int> getTotal)
		{
			var slides = course.Slides.Where(s => s.Info.UnitName == unitName).ToList();
			return new ProgressModel
			{
				Total = slides.Sum(getTotal),
				Earned = items.Select(getSlideId).Distinct().Count(slideId => slides.Any(s => s.Id == slideId))
			};
		}

		public ApplicationUser User { get; set; }
		public Course Course;
		public ProgressModel Total;
		public UserCourseUnitModel[] Units;
	}

	public class UserCourseUnitModel
	{
		public string UnitName;
		public ProgressModel Total;
		public ProgressModel Exercises;
		public ProgressModel SlideVisits;
		public ProgressModel Quizes;
	}

	public class ProgressModel
	{
		public int Earned;
		public int Total;
		public decimal? Progress { get { return Total == 0 ? null : (decimal?)Earned / Total; } }

		public override string ToString()
		{
			if (Progress.HasValue)
				return Progress.Value.ToString("0%");
			return "—";
		}
	}
}