using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Mvc;
using AntiPlagiarism.Api;
using Database.DataContexts;
using Database.Extensions;
using Database.Models;
using Elmah;
using JetBrains.Annotations;
using Microsoft.AspNet.Identity;
using Serilog;
using Serilog.Events;
using uLearn.Helpers;
using uLearn.Model.Blocks;
using uLearn.Web.Extensions;
using uLearn.Web.FilterAttributes;
using uLearn.Web.LTI;
using uLearn.Web.Models;
using Ulearn.Common.Extensions;

namespace uLearn.Web.Controllers
{
	[ULearnAuthorize]
	public class ExerciseController : BaseExerciseController
	{
		private readonly ExerciseStudentZipsCache exerciseStudentZipsCache;
		
		public ExerciseController()
		{
			exerciseStudentZipsCache = new ExerciseStudentZipsCache();
		}
		
		[System.Web.Mvc.HttpPost]
		public async Task<ActionResult> RunSolution(string courseId, Guid slideId, bool isLti = false)
		{
			/* Check that no checking solution by this user in last time */
			var halfMinuteAgo = DateTime.Now.Subtract(TimeSpan.FromSeconds(30));
			if (userSolutionsRepo.IsCheckingSubmissionByUser(courseId, slideId, User.Identity.GetUserId(), halfMinuteAgo, DateTime.MaxValue))
			{
				return Json(new RunSolutionResult
				{
					Ignored = true,
					ErrorMessage = "Ваше решение по этой задаче уже проверяется. Дождитесь окончания проверки"
				});
			}

			var code = Request.InputStream.GetString();
			if (code.Length > TextsRepo.MaxTextSize)
			{
				return Json(new RunSolutionResult
				{
					IsCompileError = true,
					ErrorMessage = "Слишком большой код"
				});
			}
			var exerciseSlide = courseManager.FindCourse(courseId)?.FindSlideById(slideId) as ExerciseSlide;
			if (exerciseSlide == null)
				return HttpNotFound();

			var result = await CheckSolution(
				courseId, exerciseSlide, code, User.Identity.GetUserId(), User.Identity.Name,
				waitUntilChecked: true, saveSubmissionOnCompileErrors: false
			);

			if (isLti)
				try
				{
					LtiUtils.SubmitScore(exerciseSlide, User.Identity.GetUserId());
				}
				catch (Exception e)
				{
					ErrorLog.GetDefault(System.Web.HttpContext.Current).Log(new Error(e));
					return Json(new RunSolutionResult
					{
						IsCompillerFailure = true,
						ErrorMessage = "Мы не смогли отправить баллы на вашу образовательную платформу. Пожалуйста, обновите страницу — мы попробуем сделать это ещё раз."
					});
				}

			return Json(result);
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		[System.Web.Mvc.HttpPost]
		[ValidateInput(false)]
		public async Task<ActionResult> AddExerciseCodeReview(string courseId, int checkingId, [FromBody] ReviewInfo reviewInfo)
		{
			var checking = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(checkingId);
			if (!string.Equals(checking.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			/* Make start position less than finish position */
			if (reviewInfo.StartLine > reviewInfo.FinishLine || (reviewInfo.StartLine == reviewInfo.FinishLine && reviewInfo.StartPosition > reviewInfo.FinishPosition))
			{
				var tmp = reviewInfo.StartLine;
				reviewInfo.StartLine = reviewInfo.FinishLine;
				reviewInfo.FinishLine = tmp;

				tmp = reviewInfo.StartPosition;
				reviewInfo.StartPosition = reviewInfo.FinishPosition;
				reviewInfo.FinishPosition = tmp;
			}

			var review = await slideCheckingsRepo.AddExerciseCodeReview(checking, User.Identity.GetUserId(), reviewInfo.StartLine, reviewInfo.StartPosition, reviewInfo.FinishLine, reviewInfo.FinishPosition, reviewInfo.Comment);

			return PartialView("_ExerciseReview", new ExerciseCodeReviewModel
			{
				Review = review,
				ManualChecking = checking,
			});
		}

		[System.Web.Mvc.HttpPost]
		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public async Task<ActionResult> DeleteExerciseCodeReview(string courseId, int reviewId)
		{
			var review = slideCheckingsRepo.FindExerciseCodeReviewById(reviewId);
			if (!string.Equals(review.ExerciseChecking.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
			if (review.AuthorId != User.Identity.GetUserId() && !User.HasAccessFor(courseId, CourseRole.CourseAdmin))
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			await slideCheckingsRepo.DeleteExerciseCodeReview(review);

			return Json(new CodeReviewOperationResult { Status = "ok" });
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		[System.Web.Mvc.HttpPost]
		[ValidateInput(false)]
		public async Task<ActionResult> UpdateExerciseCodeReview(string courseId, int reviewId, string comment)
		{
			var review = slideCheckingsRepo.FindExerciseCodeReviewById(reviewId);
			if (!string.Equals(review.ExerciseChecking.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
			if (review.AuthorId != User.Identity.GetUserId())
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			await slideCheckingsRepo.UpdateExerciseCodeReview(review, comment);

			return Json(new CodeReviewOperationResult { Status = "ok" });
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		[System.Web.Mvc.HttpPost]
		[ValidateInput(false)]
		public async Task<ActionResult> HideFromTopCodeReviewComments(string courseId, Guid slideId, string comment)
		{
			var slide = courseManager.FindCourse(courseId)?.FindSlideById(slideId) as ExerciseSlide;
			if (slide == null)
				return HttpNotFound();

			var userId = User.Identity.GetUserId();
			await slideCheckingsRepo.HideFromTopCodeReviewComments(courseId, slideId, userId, comment);

			return PartialView("_TopUserReviewComments", new ExerciseBlockData(courseId, slide)
			{
				TopUserReviewComments = slideCheckingsRepo.GetTopUserReviewComments(courseId, slideId, userId, 10)
			});
		}

		[ULearnAuthorize(MinAccessLevel = CourseRole.CourseAdmin)]
		public ActionResult SlideCodeReviewComments(string courseId, Guid slideId)
		{
			var comments = slideCheckingsRepo.GetAllReviewComments(courseId, slideId);
			return PartialView("_SlideCodeReviewComments", comments);
		}

		[System.Web.Mvc.HttpPost]
		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public async Task<ActionResult> ScoreExercise(int id, string nextUrl, string exerciseScore, bool? prohibitFurtherReview, string errorUrl = "", bool recheck = false)
		{
			if (string.IsNullOrEmpty(errorUrl))
				errorUrl = nextUrl;

			using (var transaction = db.Database.BeginTransaction())
			{
				var checking = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(id);

				var course = courseManager.GetCourse(checking.CourseId);
				var slide = (ExerciseSlide)course.GetSlideById(checking.SlideId);
				var exercise = slide.Exercise;

				/* Invalid form: score isn't integer */
				if (!int.TryParse(exerciseScore, out var score))
					return Redirect(errorUrl + "Неверное количество баллов");

				/* Invalid form: score isn't from range 0..MAX_SCORE */
				if (score < 0 || score > exercise.MaxReviewScore)
					return Redirect(errorUrl + $"Неверное количество баллов: {score}");

				checking.ProhibitFurtherManualCheckings = false;
				await slideCheckingsRepo.MarkManualCheckingAsChecked(checking, score);
				if (prohibitFurtherReview.HasValue && prohibitFurtherReview.Value)
					await slideCheckingsRepo.ProhibitFurtherExerciseManualChecking(checking);
				await visitsRepo.UpdateScoreForVisit(checking.CourseId, checking.SlideId, checking.UserId);

				transaction.Commit();

				await NotifyAboutManualExerciseChecking(checking);
			}

			return Redirect(nextUrl);
		}

		private async Task NotifyAboutManualExerciseChecking(ManualExerciseChecking checking)
		{
			var notification = new PassedManualExerciseCheckingNotification
			{
				Checking = checking,
			};
			await notificationsRepo.AddNotification(checking.CourseId, notification, User.Identity.GetUserId());
		}

		[System.Web.Mvc.HttpPost]
		[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
		public async Task<ActionResult> SimpleScoreExercise(int submissionId, int exerciseScore, bool ignoreNewestSubmission = false, int? updateCheckingId = null)
		{
			var submission = userSolutionsRepo.FindSubmissionById(submissionId);
			var courseId = submission.CourseId;
			var slideId = submission.SlideId;
			var userId = submission.UserId;

			if (!User.HasAccessFor(courseId, CourseRole.Instructor))
				return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

			if (!ignoreNewestSubmission && !updateCheckingId.HasValue)
			{
				var lastAcceptedSubmission = userSolutionsRepo.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId).OrderByDescending(s => s.Timestamp).FirstOrDefault();
				if (lastAcceptedSubmission != null && lastAcceptedSubmission.Id != submission.Id)
					return Json(
						new SimpleScoreExerciseResult
						{
							Status = "error",
							Error = "has_newest_submission",
							SubmissionId = lastAcceptedSubmission.Id,
							SubmissionDate = lastAcceptedSubmission.Timestamp.ToAgoPrettyString(true)
						});
			}

			var manualScore = slideCheckingsRepo.GetManualScoreForSlide(courseId, slideId, userId);
			if (exerciseScore < manualScore && !updateCheckingId.HasValue)
				return Json(
					new SimpleScoreExerciseResult
					{
						Status = "error",
						Error = "has_greatest_score",
						Score = manualScore.ToString(),
						CheckedQueueUrl = Url.Action("ManualExerciseCheckingQueue", "Admin", new { courseId, done = true, userId, slideId })
					});

			/* TODO: check if 0 <= exerciseScore <= exercise.MaxReviewScore */

			await slideCheckingsRepo.RemoveWaitingManualExerciseCheckings(courseId, slideId, userId);
			ManualExerciseChecking checking;
			if (updateCheckingId.HasValue)
				checking = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(updateCheckingId.Value);
			else
				checking = await slideCheckingsRepo.AddManualExerciseChecking(courseId, slideId, userId, submission);
			await slideCheckingsRepo.LockManualChecking(checking, User.Identity.GetUserId());
			await slideCheckingsRepo.MarkManualCheckingAsChecked(checking, exerciseScore);
			await visitsRepo.UpdateScoreForVisit(courseId, slideId, userId);

			await NotifyAboutManualExerciseChecking(checking);

			return Json(
				new SimpleScoreExerciseResult
				{
					Status = "ok",
					Score = exerciseScore.PluralizeInRussian(RussianPluralizationOptions.Score),
					TotalScore = visitsRepo.GetScore(slideId, userId),
					CheckingId = checking.Id,
				});
		}

		public ActionResult SubmissionsPanel(string courseId, Guid slideId, string userId = "", int? currentSubmissionId = null, bool canTryAgain = true)
		{
			if (!User.HasAccessFor(courseId, CourseRole.Instructor))
				userId = "";

			if (userId == "")
				userId = User.Identity.GetUserId();

			var slide = courseManager.GetCourse(courseId).FindSlideById(slideId);
			var submissions = userSolutionsRepo.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId).ToList();

			return PartialView(new ExerciseSubmissionsPanelModel(courseId, slide)
			{
				Submissions = submissions,
				CurrentSubmissionId = currentSubmissionId,
				CanTryAgain = canTryAgain,
			});
		}

		private ExerciseBlockData CreateExerciseBlockData(Course course, Slide slide, UserExerciseSubmission submission, bool onlyAccepted, string currentUserId)
		{
			var userId = submission?.UserId ?? currentUserId;
			var visit = visitsRepo.FindVisiter(course.Id, slide.Id, userId);

			var solution = submission?.SolutionCode.Text;
			if (string.IsNullOrEmpty(solution))
			{
				/* I.e. after click on `Try again` button solution is empty. Let try to show last sent submission */
				var lastSubmission = userSolutionsRepo.GetAllSubmissionsByUser(course.Id, slide.Id, currentUserId).OrderByDescending(s => s.Timestamp).FirstOrDefault();
				solution = lastSubmission?.SolutionCode.Text;
			}

			var submissionReviews = submission?.ManualCheckings.LastOrDefault()?.NotDeletedReviews;

			var hasUncheckedReview = submission?.ManualCheckings.Any(c => !c.IsChecked) ?? false;
			var hasCheckedReview = submission?.ManualCheckings.Any(c => c.IsChecked) ?? false;
			var reviewState = hasCheckedReview ? ExerciseReviewState.Reviewed :
				hasUncheckedReview ? ExerciseReviewState.WaitingForReview :
					ExerciseReviewState.NotReviewed;

			var submissions = onlyAccepted ?
				userSolutionsRepo.GetAllAcceptedSubmissionsByUser(course.Id, slide.Id, userId) :
				userSolutionsRepo.GetAllSubmissionsByUser(course.Id, slide.Id, userId);
			var topUserReviewComments = slideCheckingsRepo.GetTopUserReviewComments(course.Id, slide.Id, currentUserId, 10);

			return new ExerciseBlockData(course.Id, (ExerciseSlide)slide, visit?.IsSkipped ?? false, solution)
			{
				Url = Url,
				Reviews = submissionReviews?.ToList() ?? new List<ExerciseCodeReview>(),
				ReviewState = reviewState,
				IsGuest = string.IsNullOrEmpty(currentUserId),
				SubmissionSelectedByUser = submission,
				Submissions = submissions.ToList(),
				TopUserReviewComments = topUserReviewComments,
			};
		}

		private UserExerciseSubmission GetExerciseSubmissionShownByDefault(string courseId, Guid slideId, string userId, bool allowNotAccepted = false)
		{
			var submissions = userSolutionsRepo.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId).ToList();
			var lastSubmission = submissions.LastOrDefault(s => s.ManualCheckings != null && s.ManualCheckings.Any()) ??
								submissions.LastOrDefault(s => s.AutomaticCheckingIsRightAnswer);
			if (lastSubmission == null && allowNotAccepted)
				lastSubmission = userSolutionsRepo.GetAllSubmissionsByUser(courseId, slideId, userId).ToList().LastOrDefault();
			return lastSubmission;
		}

		[System.Web.Mvc.AllowAnonymous]
		public ActionResult Submission(string courseId, Guid slideId, string userId = null, int? submissionId = null, int? manualCheckingId = null, bool isLti = false, bool showOutput = false, bool instructorView = false, bool onlyAccepted = true)
		{
			if (!User.HasAccessFor(courseId, CourseRole.Instructor))
				instructorView = false;

			var currentUserId = userId ?? (User.Identity.IsAuthenticated ? User.Identity.GetUserId() : "");
			UserExerciseSubmission submission = null;
			if (submissionId.HasValue && submissionId.Value > 0)
			{
				submission = userSolutionsRepo.FindSubmissionById(submissionId.Value);
				if (submission == null)
					return HttpNotFound();
				if (!string.Equals(courseId, submission.CourseId, StringComparison.OrdinalIgnoreCase))
					return HttpNotFound();
				if (slideId != submission.SlideId)
					return HttpNotFound();
				if (!User.HasAccessFor(courseId, CourseRole.Instructor) && submission.UserId != currentUserId)
					return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
			}
			else if (!submissionId.HasValue && !manualCheckingId.HasValue)
			{
				submission = GetExerciseSubmissionShownByDefault(courseId, slideId, currentUserId, instructorView);
			}

			var course = courseManager.GetCourse(courseId);
			var slide = course.FindSlideById(slideId);
			if (slide == null)
				return HttpNotFound();

			ManualExerciseChecking manualChecking = null;
			if (User.HasAccessFor(courseId, CourseRole.Instructor) && manualCheckingId.HasValue)
			{
				manualChecking = slideCheckingsRepo.FindManualCheckingById<ManualExerciseChecking>(manualCheckingId.Value);
			}
			if (manualChecking != null && !submissionId.HasValue)
				submission = manualChecking.Submission;

			var model = CreateExerciseBlockData(course, slide, submission, onlyAccepted, currentUserId);
			model.IsLti = isLti;
			model.ShowOutputImmediately = showOutput;
			model.InstructorView = instructorView;
			model.ShowOnlyAccepted = onlyAccepted;
			if (manualChecking != null)
			{
				if (string.Equals(manualChecking.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
				{
					model.ManualChecking = manualChecking;
					model.Reviews = submission?.ManualCheckings.SelectMany(c => c.NotDeletedReviews).ToList() ?? new List<ExerciseCodeReview>();
				}
			}

			return PartialView(model);
		}

        [System.Web.Mvc.AllowAnonymous]
		public ActionResult StudentZip(string courseId, Guid? slideId)
		{
			if (!slideId.HasValue)
				return HttpNotFound();
			var slide = courseManager.FindCourse(courseId)?.FindSlideById(slideId.Value);
			if (!(slide is ExerciseSlide))
				return HttpNotFound();

			var exerciseSlide = slide as ExerciseSlide;
			if (!(exerciseSlide.Exercise is ProjectExerciseBlock))
				return HttpNotFound();

			var block = (ProjectExerciseBlock) exerciseSlide.Exercise;
			var zipFile = exerciseStudentZipsCache.GenerateOrFindZip(courseId, exerciseSlide);
			
			return File(zipFile.FullName, "application/zip", block.CsprojFile.Name + ".zip");
		}
	}

	[DataContract]
	public class CodeReviewOperationResult
	{
		[DataMember(Name = "status")]
		public string Status { get; set; }
	}

	[DataContract]
	public class SimpleScoreExerciseResult
	{
		[DataMember(Name = "status")]
		public string Status { get; set; }

		[DataMember(Name = "error")]
		public string Error { get; set; }

		[DataMember(Name = "submissionId")]
		public int SubmissionId { get; set; }

		[DataMember(Name = "submissionDate")]
		public string SubmissionDate { get; set; }

		[DataMember(Name = "score")]
		public string Score { get; set; }

		[DataMember(Name = "totalScore")]
		public int TotalScore { get; set; }

		[DataMember(Name = "checkedQueueUrl")]
		public string CheckedQueueUrl { get; set; }

		[DataMember(Name = "checkingId")]
		public int CheckingId { get; set; }
	}

	public class ReviewInfo
	{
		[AllowHtml]
		public string Comment { get; set; }

		public int StartLine { get; set; }
		public int StartPosition { get; set; }
		public int FinishLine { get; set; }
		public int FinishPosition { get; set; }
	}

	public class ExerciseSubmissionsPanelModel
	{
		public ExerciseSubmissionsPanelModel(string courseId, Slide slide)
		{
			CourseId = courseId;
			Slide = slide;

			Submissions = new List<UserExerciseSubmission>();
			CurrentSubmissionId = null;
			CanTryAgain = true;
			ShowButtons = true;
			SelectControlName = "version";
		}

		public string CourseId { get; set; }
		public Slide Slide { get; set; }
		public List<UserExerciseSubmission> Submissions { get; set; }
		public int? CurrentSubmissionId { get; set; }
		public bool CanTryAgain { get; set; }
		public bool ShowButtons { get; set; }
		public string SelectControlName { get; set; }

		[CanBeNull]
		public Func<UserExerciseSubmission, string> GetSubmissionDescription { get; set; }
		
		/* By default it's Url.RouteUrl("Course.SlideById", new { Model.CourseId, slideId = Model.Slide.Url }) */
		[CanBeNull]
		public string FormUrl { get; set; }

	}

	public class ExerciseControlsModel
	{
		public ExerciseControlsModel(string courseId, ExerciseSlide slide)
		{
			CourseId = courseId;
			Slide = slide;
		}

		public string CourseId;
		public ExerciseSlide Slide;
		public ExerciseBlock Block => Slide.Exercise;

		public bool IsLti = false;
		public bool IsCodeEditableAndSendable = true;
		public bool DebugView = false;
		public bool CanShowOutput = false;
		public bool IsShowOutputButtonActive = false;
		public string AcceptedSolutionsAction = "";
		public string RunSolutionUrl = "";
		public string UseHintUrl = "";

		public bool HideShowSolutionsButton => Block.HideShowSolutionsButton;
	}

	public class ExerciseScoreFormModel
	{
		public ExerciseScoreFormModel(string courseId, ExerciseSlide slide, ManualExerciseChecking checking, List<string> groupsIds = null,
			bool isCurrentSubmissionChecking = false, bool defaultProhibitFutherReview = true)
		{
			CourseId = courseId;
			Slide = slide;
			Checking = checking;
			GroupsIds = groupsIds;
			IsCurrentSubmissionChecking = isCurrentSubmissionChecking;
			DefaultProhibitFutherReview = defaultProhibitFutherReview;
		}
		
		public string CourseId { get; set; }
		public ExerciseSlide Slide { get; set; }
		public ManualExerciseChecking Checking { get; set; }
		public List<string> GroupsIds { get; set; }
		public string GroupsIdsJoined => string.Join(",", GroupsIds ?? new List<string>());
		public bool IsCurrentSubmissionChecking { get; set; }
		public bool DefaultProhibitFutherReview { get; set; }
	}
}