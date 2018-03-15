using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;
using Database;
using Database.DataContexts;
using Database.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using uLearn.Web.FilterAttributes;
using Ulearn.Common.Extensions;

namespace uLearn.Web.Controllers
{
	[ULearnAuthorize(MinAccessLevel = CourseRole.Instructor)]
	public class AntiPlagiarismController : JsonDataContractController
	{
		private readonly ULearnDb db;
		private readonly UserSolutionsRepo userSolutionsRepo;
		private readonly CourseManager courseManager;		
		private static readonly IAntiPlagiarismClient antiPlagiarismClient;

		static AntiPlagiarismController()
		{
			var serilogLogger = new LoggerConfiguration().WriteTo.Log4Net().CreateLogger();
			var antiPlagiarismEndpointUrl = WebConfigurationManager.AppSettings["ulearn.antiplagiarism.endpoint"];
			var antiPlagiarismToken = WebConfigurationManager.AppSettings["ulearn.antiplagiarism.token"];
			antiPlagiarismClient = new AntiPlagiarismClient(antiPlagiarismEndpointUrl, antiPlagiarismToken, serilogLogger);
		}

		public AntiPlagiarismController(ULearnDb db, UserSolutionsRepo userSolutionsRepo)
		{
			this.db = db;
			this.userSolutionsRepo = userSolutionsRepo;
		}

		public AntiPlagiarismController(ULearnDb db, CourseManager courseManager)
			:this(db, new UserSolutionsRepo(db, courseManager))
		{
			this.courseManager = courseManager;
		}
		
		public AntiPlagiarismController()
			: this(new ULearnDb(), WebCourseManager.Instance)
		{
		}

		public async Task<ActionResult> Info(string courseId, int submissionId)
		{
			var submission = userSolutionsRepo.FindSubmissionById(submissionId);
			if (!string.Equals(submission.CourseId, courseId, StringComparison.InvariantCultureIgnoreCase))
				return HttpNotFound();
			
			var antiPlagiarismsResult = await antiPlagiarismClient.GetAuthorPlagiarismsAsync(new GetAuthorPlagiarismsParameters
			{
				AuthorId = Guid.Parse(submission.UserId),
				TaskId = submission.SlideId
			});

			var model = new AntiPlagiarismInfoModel
			{
				SuspicionLevel = SuspicionLevel.None,
				SuspiciousSubmissionsCount = 0,
			};
			foreach (var researchedSubmission in antiPlagiarismsResult.ResearchedSubmissions)
			{
				foreach (var plagiarism in researchedSubmission.Plagiarisms)
				{
					if (plagiarism.Weight >= antiPlagiarismsResult.SuspicionLevels.StrongSuspicion)
						model.SuspicionLevel = SuspicionLevel.Strong;
					else if (plagiarism.Weight >= antiPlagiarismsResult.SuspicionLevels.FaintSuspicion && model.SuspicionLevel == SuspicionLevel.None)
						model.SuspicionLevel = SuspicionLevel.Faint;

					model.SuspiciousSubmissionsCount++;
				}
			}

			return Json(model, JsonRequestBehavior.AllowGet);
		}

		public async Task<ActionResult> Details(string courseId, int submissionId)
		{
			var submission = userSolutionsRepo.FindSubmissionById(submissionId);
			if (!string.Equals(submission.CourseId, courseId, StringComparison.InvariantCultureIgnoreCase))
				return HttpNotFound(); 
					
			var antiPlagiarismsResult = await antiPlagiarismClient.GetAuthorPlagiarismsAsync(new GetAuthorPlagiarismsParameters
			{
				AuthorId = Guid.Parse(submission.UserId),
				TaskId = submission.SlideId
			});

			var antiPlagiarismSubmissionsIds = antiPlagiarismsResult.ResearchedSubmissions.Select(s => s.SubmissionInfo.Id);
			var plagiarismsSubmissionsIds = antiPlagiarismsResult.ResearchedSubmissions
				.SelectMany(s => s.Plagiarisms)
				.Select(s => s.SubmissionInfo.Id);

			var submissions = userSolutionsRepo.GetSubmissionsByAntiPlagiarismSubmissionsIds(
					antiPlagiarismSubmissionsIds.Concat(plagiarismsSubmissionsIds)
					)
				.ToDictionary(s => s.Id);
			submissions[submissionId] = submission;

			var course = courseManager.FindCourse(courseId);
			var slide = course?.FindSlideById(submission.SlideId);
			return View(new AntiPlagiarismDetailsModel
			{
				Course = course,
				Slide = slide,
				SubmissionId = submissionId,
				Submissions = submissions,
				AntiPlagiarismResult = antiPlagiarismsResult,
			});
		}
		
		public ActionResult SubmissionsPanel(int submissionId, Dictionary<int, double> submissionWeights)
		{
			var submission = userSolutionsRepo.FindSubmissionById(submissionId);
			if (submission == null)
				return HttpNotFound();

			var courseId = submission.CourseId;
			var slideId = submission.SlideId;
			var userId = submission.UserId;
			var slide = courseManager.FindCourse(courseId)?.FindSlideById(slideId);
			if (slide == null)
				return HttpNotFound();
			var submissions = userSolutionsRepo.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId).ToList();

			return PartialView("~/Views/Exercise/SubmissionsPanel.cshtml", new ExerciseSubmissionsPanelModel(courseId, slide)
			{
				Submissions = submissions,
				CurrentSubmissionId = submissionId,
				CanTryAgain = false,
				GetSubmissionDescription = s => GetSubmissionDescriptionForPanel(submissionWeights.ContainsKey(s.Id) ? (double?) submissionWeights[s.Id] : null),
				FormUrl = Url.Action("Details", new { courseId }),
				ShowButtons = false,
				SelectControlName = "submissionId",
			});
		}

		private static string GetSubmissionDescriptionForPanel(double? submissionWeight)
		{
			if (!submissionWeight.HasValue)
				return ", не проверялась на списывание";
			var weightPercents = (int)(submissionWeight.Value * 100);
			return $", подозрительность — {weightPercents}%";
		}
	}

	[JsonConverter(typeof(StringEnumConverter), true)]
	public enum SuspicionLevel : short
	{
		None = 0,
		Faint = 1,
		Strong = 2,
	}
	
	[DataContract]
	public class AntiPlagiarismInfoModel
	{
		[DataMember(Name = "suspicion_level")]
		public SuspicionLevel SuspicionLevel { get; set; }
		
		[DataMember(Name = "suspicious_submissions_count")]
		public int SuspiciousSubmissionsCount { get; set; }
	}

	public class AntiPlagiarismDetailsModel
	{
		public int SubmissionId { get; set; }
		
		public Course Course { get; set; }
		
		public Slide Slide { get; set; }
		
		public GetAuthorPlagiarismsResult AntiPlagiarismResult { get; set; }
		
		public Dictionary<int, UserExerciseSubmission> Submissions { get; set; }
	}
}