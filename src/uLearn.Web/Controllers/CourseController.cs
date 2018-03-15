using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Database;
using Database.DataContexts;
using Database.Extensions;
using Database.Models;
using Elmah;
using log4net;
using LtiLibrary.Owin.Security.Lti;
using uLearn.Model.Blocks;
using uLearn.Quizes;
using uLearn.Web.Extensions;
using uLearn.Web.FilterAttributes;
using uLearn.Web.LTI;
using uLearn.Web.Models;
using Ulearn.Common.Extensions;

namespace uLearn.Web.Controllers
{
	[ULearnAuthorize]
	public class CourseController : BaseController
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(CourseController));

		private readonly ULearnDb db = new ULearnDb();
		private readonly CourseManager courseManager = WebCourseManager.Instance;

		private readonly SlideRateRepo slideRateRepo;
		private readonly UserSolutionsRepo solutionsRepo;
		private readonly UnitsRepo unitsRepo;
		private readonly VisitsRepo visitsRepo;
		private readonly LtiRequestsRepo ltiRequestsRepo;
		private readonly SlideCheckingsRepo slideCheckingsRepo;
		private readonly GroupsRepo groupsRepo;
		private readonly UserQuizzesRepo userQuizzesRepo;
		private readonly CoursesRepo coursesRepo;

		public CourseController()
		{
			slideCheckingsRepo = new SlideCheckingsRepo(db);
			visitsRepo = new VisitsRepo(db);
			unitsRepo = new UnitsRepo(db);
			slideRateRepo = new SlideRateRepo(db);
			solutionsRepo = new UserSolutionsRepo(db, courseManager);
			ltiRequestsRepo = new LtiRequestsRepo(db);
			groupsRepo = new GroupsRepo(db, courseManager);
			userQuizzesRepo = new UserQuizzesRepo(db);
			coursesRepo = new CoursesRepo(db);
		}

		[AllowAnonymous]
		public async Task<ActionResult> SlideById(string courseId, string slideId = "", int? checkQueueItemId = null, int? version = null, int autoplay = 0)
		{
			if (slideId.Contains("_"))
				slideId = slideId.Substring(slideId.LastIndexOf('_') + 1);

			var groupsIds = Request.GetMultipleValues("group");

			Guid slideGuid;
			if (!Guid.TryParse(slideId, out slideGuid))
				return HttpNotFound();

			if (string.IsNullOrWhiteSpace(courseId))
			{
				if (string.IsNullOrWhiteSpace(slideId))
					return RedirectToAction("Index", "Home");

				return RedirectToSlideById(slideGuid);
			}

			var course = courseManager.FindCourse(courseId);
			if (course == null)
				return HttpNotFound();

			var visibleUnits = unitsRepo.GetVisibleUnits(course, User);
			var isGuest = !User.Identity.IsAuthenticated;

			var slide = slideGuid == Guid.Empty ? GetInitialSlideForStartup(courseId, course, visibleUnits) : course.FindSlideById(slideGuid);

			if (slide == null)
				return HttpNotFound();

			AbstractManualSlideChecking queueItem = null;
			var isManualCheckingReadonly = false;
			if (User.HasAccessFor(courseId, CourseRole.Instructor) && checkQueueItemId != null)
			{
				if (slide is QuizSlide)
					queueItem = slideCheckingsRepo.FindManualCheckingById<ManualQuizChecking>(checkQueueItemId.Value);
				if (slide is ExerciseSlide)
					queueItem = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(checkQueueItemId.Value);

				if (queueItem == null)
					return HttpNotFound();
			}

			var model = isGuest ?
				CreateGuestCoursePageModel(course, slide, autoplay > 0) :
				await CreateCoursePageModel(course, slide, queueItem, version, groupsIds, autoplay > 0, isManualCheckingReadonly);

			if (!string.IsNullOrEmpty(Request.QueryString["error"]))
				model.Error = Request.QueryString["error"];

			if (!visibleUnits.Contains(model.Slide.Info.Unit))
				return HttpNotFound("Slide is hidden " + slideGuid);
			return View("Slide", model);
		}

		private ActionResult RedirectToSlideById(Guid slideGuid)
		{
			var course = courseManager.FindCourseBySlideById(slideGuid);
			if (course == null)
				return HttpNotFound();
			var slide = course.GetSlideById(slideGuid);
			return RedirectToRoute("Course.SlideById", new { courseId = course.Id, slideId = slide.Url });
		}

		private string GetAdminQueueActionName(AbstractManualSlideChecking queueItem)
		{
			if (queueItem is ManualQuizChecking)
				return "ManualQuizCheckingQueue";
			if (queueItem is ManualExerciseChecking)
				return "ManualExerciseCheckingQueue";
			return "";
		}

		private string GetAdminCheckActionName(AbstractManualSlideChecking queueItem)
		{
			if (queueItem is ManualQuizChecking)
				return "CheckQuiz";
			if (queueItem is ManualExerciseChecking)
				return "CheckExercise";
			return "";
		}

		[AllowAnonymous]
		public ActionResult Slide(string courseId, int slideIndex = -1)
		{
			var course = courseManager.FindCourse(courseId);
			if (course == null)
				return HttpNotFound();
			var visibleUnits = unitsRepo.GetVisibleUnits(course, User);
			var slide = slideIndex == -1 ? GetInitialSlideForStartup(courseId, course, visibleUnits) : course.Slides[slideIndex];
			return RedirectToRoute("Course.SlideById", new { courseId, slideId = slide.Url });
		}

		[AllowAnonymous]
		public async Task<ActionResult> LtiSlide(string courseId, Guid slideId)
		{
			if (string.IsNullOrWhiteSpace(courseId))
				return RedirectToAction("Index", "Home");

			var course = courseManager.GetCourse(courseId);
			var slide = course.GetSlideById(slideId);

			string userId;
			var owinRequest = Request.GetOwinContext().Request;
			if (await owinRequest.IsAuthenticatedLtiRequestAsync())
			{
				var ltiRequest = await owinRequest.ParseLtiRequestAsync();
				log.Info($"Нашёл LTI request в запросе: {ltiRequest.JsonSerialize()}");
				userId = Request.GetOwinContext().Authentication.AuthenticationResponseGrant.Identity.GetUserId();
				await ltiRequestsRepo.Update(userId, slide.Id, ltiRequest.JsonSerialize());

				/* Substitute http(s) scheme with real scheme from header */
				var uriBuilder = new UriBuilder(ltiRequest.Url)
				{
					Scheme = owinRequest.GetRealRequestScheme(),
					Port = owinRequest.GetRealRequestPort()
				};
				return Redirect(uriBuilder.Uri.AbsoluteUri);
			}

			/* For now user should be authenticated */
			if (!User.Identity.IsAuthenticated)
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			userId = User.Identity.GetUserId();
			var visit = await VisitSlide(courseId, slide.Id, userId);

			/* Try to send score via LTI immediately after slide visiting */
			try
			{
				if (visit.IsPassed)
					LtiUtils.SubmitScore(slide, userId, visit);
			}
			catch (Exception e)
			{
				ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e));
			}

			var exerciseSlide = slide as ExerciseSlide;
			if (exerciseSlide != null)
			{
				var model = new CodeModel
				{
					CourseId = courseId,
					SlideId = exerciseSlide.Id,
					ExerciseBlock = exerciseSlide.Exercise,
					Context = CreateRenderContext(course, exerciseSlide, isLti: true)
				};
				return View("LtiExerciseSlide", model);
			}

			var quizSlide = slide as QuizSlide;
			if (quizSlide != null)
			{
				var model = new LtiQuizModel
				{
					CourseId = courseId,
					Slide = quizSlide,
					UserId = userId
				};
				return View("LtiQuizSlide", model);
			}

			return View();
		}

		private Slide GetInitialSlideForStartup(string courseId, Course course, List<Unit> visibleUnits)
		{
			var userId = User.Identity.GetUserId();
			var visitedIds = visitsRepo.GetIdOfVisitedSlides(courseId, userId);
			var visibleSlides = visibleUnits.SelectMany(u => u.Slides).ToList();
			var lastVisited = visibleSlides.LastOrDefault(slide => visitedIds.Contains(slide.Id));
			if (lastVisited == null)
				return visibleSlides.Any() ? visibleSlides.First() : null;

			var unitSlides = lastVisited.Info.Unit.Slides.Where(s => visibleSlides.Contains(s)).ToList();

			var lastVisitedSlide = unitSlides.First();
			foreach (var slide in unitSlides)
			{
				if (visitedIds.Contains(slide.Id))
					lastVisitedSlide = slide;
				else
					return lastVisitedSlide;
			}
			return lastVisitedSlide;
		}

		private CoursePageModel CreateGuestCoursePageModel(Course course, Slide slide, bool autoplay)
		{
			return new CoursePageModel
			{
				CourseId = course.Id,
				CourseTitle = course.Title,
				Slide = slide,
				Score = Tuple.Create(0, 0),
				BlockRenderContext = new BlockRenderContext(
					course,
					slide,
					slide.Info.DirectoryRelativePath,
					slide.Blocks.Select(block => block is ExerciseBlock ? new ExerciseBlockData(course.Id, (ExerciseSlide)slide, false) { Url = Url } : (dynamic)null).ToArray(),
					true,
					autoplay: autoplay),
				IsGuest = true,
			};
		}

		private async Task<CoursePageModel> CreateCoursePageModel(
			Course course, Slide slide,
			AbstractManualSlideChecking manualChecking, int? exerciseSubmissionId = null,
			List<string> groupsIds = null,
			bool autoplay = false,
			bool isManualCheckingReadonly = false)
		{
			var userId = User.Identity.GetUserId();

			if (manualChecking != null)
				userId = manualChecking.UserId;

			var visiter = await VisitSlide(course.Id, slide.Id, userId);
			var maxSlideScore = GetMaxSlideScoreForUser(course, slide, userId);
			var defaultProhibitFutherReview = groupsRepo.GetDefaultProhibitFutherReviewForUser(course.Id, userId, User);

			var score = Tuple.Create(visiter.Score, maxSlideScore);
			var model = new CoursePageModel
			{
				UserId = userId,
				CourseId = course.Id,
				CourseTitle = course.Title,
				Slide = slide,
				Score = score,
				BlockRenderContext = CreateRenderContext(course, slide, manualChecking, exerciseSubmissionId, groupsIds, autoplay: autoplay, isManualCheckingReadonly: isManualCheckingReadonly, defaultProhibitFutherReview: defaultProhibitFutherReview),
				ManualChecking = manualChecking,
				ContextManualCheckingUserGroups = manualChecking != null ? groupsRepo.GetUserGroupsNamesAsString(course.Id, manualChecking.UserId, User) : "",
				IsGuest = false
			};
			return model;
		}

		private int GetMaxSlideScoreForUser(Course course, Slide slide, string userId)
		{
			var solvedSlidesIds = ControllerUtils.GetSolvedSlides(solutionsRepo, userQuizzesRepo, course, userId);
			var slidesWithUsersManualChecking = new HashSet<Guid>(visitsRepo.GetSlidesWithUsersManualChecking(course.Id, userId));
			var enabledManualCheckingForUser = groupsRepo.IsManualCheckingEnabledForUser(course, userId);
			var maxSlideScore = ControllerUtils.GetMaxScoreForUsersSlide(slide, solvedSlidesIds.Contains(slide.Id), slidesWithUsersManualChecking.Contains(slide.Id), enabledManualCheckingForUser);
			return maxSlideScore;
		}

		private BlockRenderContext CreateRenderContext(Course course, Slide slide, 
			AbstractManualSlideChecking manualChecking = null, int? exerciseSubmissionId = null, List<string> groupsIds = null, bool isLti = false,
			bool autoplay = false, bool isManualCheckingReadonly = false, bool defaultProhibitFutherReview = true)
		{
			/* ExerciseController will fill blockDatas later */
			var blockData = slide.Blocks.Select(b => (dynamic)null).ToArray();
			return new BlockRenderContext(
				course,
				slide,
				slide.Info.DirectoryRelativePath,
				blockData,
				false,
				User.HasAccessFor(course.Id, CourseRole.Instructor),
				manualChecking,
				false,
				groupsIds,
				isLti,
				autoplay,
				isManualCheckingReadonly,
				defaultProhibitFutherReview
			)
			{
				VersionId = exerciseSubmissionId
			};
		}

		public async Task<ActionResult> AcceptedSolutions(string courseId, Guid slideId, bool isLti = false)
		{
			var course = courseManager.GetCourse(courseId);
			var slide = course.GetSlideById(slideId) as ExerciseSlide;
			if (slide == null)
				return HttpNotFound();

			// Test redirect to SlideId if disabled
			if (slide.Exercise.HideShowSolutionsButton)
				return RedirectToRoute("Course.SlideById", new { courseId = course.Id, slideId = slide.Url });
			var model = await CreateAcceptedSolutionsModel(course, slide, isLti);
			return View("AcceptedSolutions", model);
		}

		private async Task<AcceptedSolutionsPageModel> CreateAcceptedSolutionsModel(Course course, ExerciseSlide slide, bool isLti)
		{
			var userId = User.Identity.GetUserId();
			var isPassed = visitsRepo.IsPassed(slide.Id, userId);
			if (!isPassed)
				await visitsRepo.SkipSlide(course.Id, slide.Id, userId);
			var submissions = solutionsRepo.GetBestTrendingAndNewAcceptedSolutions(course.Id, slide.Id);
			foreach (var submission in submissions)
			{
				submission.LikedAlready = submission.UsersWhoLike.Any(u => u == userId);
				submission.RemoveSolutionUrl = Url.Action("RemoveSubmission", "Course", new { course.Id, slideId = slide.Id, submissionId = submission.Id });
			}

			var model = new AcceptedSolutionsPageModel
			{
				CourseId = course.Id,
				CourseTitle = course.Title,
				Slide = slide,
				AcceptedSolutions = submissions,
				User = User,
				LikeSolutionUrl = Url.Action("LikeSolution"),
				IsLti = isLti,
				IsPassed = isPassed
			};
			return model;
		}

		[AllowAnonymous]
		public async Task<ActionResult> AcceptedAlert(string courseId, Guid slideId)
		{
			var owinRequest = Request.GetOwinContext().Request;
			if (await owinRequest.IsAuthenticatedLtiRequestAsync())
			{
				var ltiRequest = await owinRequest.ParseLtiRequestAsync();
				/* Substitute http(s) scheme with real scheme from header */
				var uriBuilder = new UriBuilder(ltiRequest.Url)
				{
					Scheme = owinRequest.GetRealRequestScheme(),
					Port = owinRequest.GetRealRequestPort()
				};
				return Redirect(uriBuilder.Uri.AbsoluteUri);
			}

			/* For now user should be authenticated */
			if (!User.Identity.IsAuthenticated)
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			var course = courseManager.GetCourse(courseId);
			var slide = (ExerciseSlide)course.GetSlideById(slideId);
			var model = CreateAcceptedAlertModel(slide, course);
			return View(model);
		}

		private ExerciseBlockData CreateAcceptedAlertModel(ExerciseSlide slide, Course course)
		{
			var userId = User.Identity.GetUserId();
			var isSkippedOrPassed = visitsRepo.IsSkippedOrPassed(slide.Id, userId);
			/* TODO: It's not nesessary create ExerciseBlockData here */
			var model = new ExerciseBlockData(course.Id, slide)
			{
				IsSkippedOrPassed = isSkippedOrPassed,
				CourseId = course.Id,
				IsGuest = !User.Identity.IsAuthenticated,
				Url = Url,
			};
			return model;
		}

		/* Slide rating don't used anymore */
		[HttpPost]
		public async Task<string> ApplyRate(string courseId, Guid slideId, string rate)
		{
			var userId = User.Identity.GetUserId();
			var slideRate = (SlideRates)Enum.Parse(typeof(SlideRates), rate);
			return await slideRateRepo.AddRate(courseId, slideId, userId, slideRate);
		}

		[HttpPost]
		public string GetRate(string courseId, Guid slideId)
		{
			var userId = User.Identity.GetUserId();
			return slideRateRepo.FindRate(courseId, slideId, userId);
		}

		[HttpPost]
		public async Task<JsonResult> LikeSolution(int solutionId)
		{
			var res = await solutionsRepo.Like(solutionId, User.Identity.GetUserId());
			return Json(new { likesCount = res.Item1, liked = res.Item2 });
		}

		public async Task<Visit> VisitSlide(string courseId, Guid slideId, string userId)
		{
			if (string.IsNullOrEmpty(userId))
				return null;
			await visitsRepo.AddVisit(courseId, slideId, userId);
			return visitsRepo.FindVisiter(courseId, slideId, userId);
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public ActionResult InstructorNote(string courseId, Guid unitId)
		{
			var course = courseManager.GetCourse(courseId);
			var instructorNote = course.GetUnitById(unitId).InstructorNote;
			if (instructorNote == null)
				return HttpNotFound("No instructor note for this unit");
			return View(new IntructorNoteModel(courseId, instructorNote));
		}

		[HttpPost]
		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public async Task<ActionResult> RemoveSubmission(string courseId, Guid slideId, int submissionId)
		{
			var submission = solutionsRepo.FindSubmissionById(submissionId);
			if (submission != null)
			{
				await solutionsRepo.RemoveSubmission(submission);
				await visitsRepo.UpdateScoreForVisit(courseId, submission.SlideId, submission.UserId);
			}
			return RedirectToAction("AcceptedSolutions", new { courseId, slideId });
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Tester)]
		public async Task<ActionResult> ForgetAll(string courseId, Guid slideId)
		{
			var slide = courseManager.GetCourse(courseId).GetSlideById(slideId);
			var userId = User.Identity.GetUserId();
			db.SolutionLikes.RemoveRange(db.SolutionLikes.Where(q => q.UserId == userId && q.Submission.SlideId == slideId));

			db.UserExerciseSubmissions.RemoveSlideAction(slideId, userId);
			db.UserQuizzes.RemoveSlideAction(slideId, userId);
			db.Visits.RemoveSlideAction(slideId, userId);
			await slideCheckingsRepo.RemoveAttempts(courseId, slideId, userId, false);

			db.UserQuestions.RemoveSlideAction(slideId, userId);
			db.SlideRates.RemoveSlideAction(slideId, userId);
			db.Hints.RemoveSlideAction(slideId, userId);
			await db.SaveChangesAsync();

			return RedirectToAction("SlideById", new { courseId, slideId = slide.Id });
		}

		public ActionResult CourseInstructorNavbar(string courseId)
		{
			if (string.IsNullOrEmpty(courseId) || !User.HasAccessFor(courseId, CourseRole.Instructor))
				return PartialView((CourseInstructorNavbarViewModel)null);

			var course = courseManager.GetCourse(courseId);
			var canAddInstructors = coursesRepo.HasCourseAccess(User.Identity.GetUserId(), courseId, CourseAccessType.AddAndRemoveInstructors);
			return PartialView(new CourseInstructorNavbarViewModel
			{
				CourseId = courseId,
				CourseTitle = course.Title,
				CanAddInstructors = canAddInstructors,
			});
		}
	}
}