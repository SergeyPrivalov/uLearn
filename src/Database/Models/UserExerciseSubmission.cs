using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models
{
	public class UserExerciseSubmission : ITimedSlideAction
	{
		[Required]
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[StringLength(40)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndUser", 3)]
		public string UserId { get; set; }

		public virtual ApplicationUser User { get; set; }

		[Required]
		[StringLength(40)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndUser", 1)]
		[Index("IDX_UserExerciseSubmissions_ByCourseAndSlide", 1)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndTime", 1)]
		[Index("IDX_UserExerciseSubmissions_ByCourseAndIsRightAnswer", 1)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndIsRightAnswer", 1)]
		public string CourseId { get; set; }

		[Required]
		[Index("IDX_UserExerciseSubmissions_BySlideAndUser", 2)]
		[Index("IDX_UserExerciseSubmissions_ByCourseAndSlide", 2)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndTime", 2)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndIsRightAnswer", 2)]
		public Guid SlideId { get; set; }

		[Required]
		[Index("IDX_UserExerciseSubmissions_BySlideAndTime", 3)]
		public DateTime Timestamp { get; set; }

		[Required]
		[StringLength(40)]
		public string SolutionCodeHash { get; set; }

		public virtual TextBlob SolutionCode { get; set; }

		[Required]
		public int CodeHash { get; set; }

		public virtual IList<Like> Likes { get; set; }

		public int AutomaticCheckingId { get; set; }

		[Required]
		public virtual AutomaticExerciseChecking AutomaticChecking { get; set; }

		[Index("IDX_UserExerciseSubmissions_ByCourseAndIsRightAnswer", 2)]
		[Index("IDX_UserExerciseSubmissions_BySlideAndIsRightAnswer", 3)]
		[Index("IDX_UserExerciseSubmissions_ByIsRightAnswer")]
		public bool AutomaticCheckingIsRightAnswer { get; set; }

		public virtual IList<ManualExerciseChecking> ManualCheckings { get; set; }
		
		[Index("IDX_UserExerciseSubmission_ByAntiPlagiarismSubmissionId")]
		public int? AntiPlagiarismSubmissionId { get; set; }

		public bool IsWebSubmission => string.Equals(CourseId, "web", StringComparison.OrdinalIgnoreCase) && SlideId == Guid.Empty;
	}
}