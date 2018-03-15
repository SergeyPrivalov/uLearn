using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AntiPlagiarism.Web.Database.Models
{
	public class SnippetOccurence
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		
		public int SubmissionId { get; set; }
		public virtual Submission Submission { get; set; }
		
		public int SnippetId { get; set; }
		public virtual Snippet Snippet { get; set; }
		
		public int FirstTokenIndex { get; set; }

		public override string ToString()
		{
			return $"SnippetOccurence({Snippet}, SubmissionId={SubmissionId}, FirstTokenIndex={FirstTokenIndex})";
		}
	}
}