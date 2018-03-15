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
		[StringLength(64)]
		public string UserId { get; set; }

		public virtual ApplicationUser User { get; set; }

		[Required]
		[StringLength(100)]
		public string CourseId { get; set; }

		[Required]
		public Guid SlideId { get; set; }

		[Required]
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

		public bool AutomaticCheckingIsRightAnswer { get; set; }

		public virtual IList<ManualExerciseChecking> ManualCheckings { get; set; }
		
		public int? AntiPlagiarismSubmissionId { get; set; }

		public bool IsWebSubmission => string.Equals(CourseId, "web", StringComparison.OrdinalIgnoreCase) && SlideId == Guid.Empty;
	}
}