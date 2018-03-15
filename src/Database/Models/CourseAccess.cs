using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Models
{
	public class CourseAccess
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		
		[Required]
		[StringLength(64)]
		[Index("IDX_CourseAccess_ByCourse")]
		[Index("IDX_CourseAccess_ByCourseAndIsEnabled", 1)]
		[Index("IDX_CourseAccess_ByCourseUserAndIsEnabled", 1)]
		public string CourseId { get; set; }

		[StringLength(64)]
		[Index("IDX_CourseAccess_ByUser")]
		[Index("IDX_CourseAccess_ByCourseUserAndIsEnabled", 2)]
		public string UserId { get; set; }

		public virtual ApplicationUser User { get; set; }

		[StringLength(64)]
		public string GrantedById { get; set; }

		public virtual ApplicationUser GrantedBy { get; set; }

		[Required]
		public CourseAccessType AccessType { get; set; }

		[Index("IDX_CourseAccess_ByGrantTime")]
		public DateTime GrantTime { get; set; }

		[Required]
		[Index("IDX_CourseAccess_ByCourseAndIsEnabled", 2)]
		[Index("IDX_CourseAccess_ByCourseUserAndIsEnabled", 3)]
		public bool IsEnabled { get; set; }
	}

	public enum CourseAccessType : short
	{
		/* Редактировать, закреплять, удалять (скрывать) комментарии */
		[Display(Name = "Редактировать и удалять комментарии")]
		EditPinAndRemoveComments = 1,

		/* Смотреть решения всех, а не только студентов своих групп */
		[Display(Name = "Видеть решения всех пользователей")]
		ViewAllStudentsSubmissions = 2,

		/* Назначать людей преподавателями */
		[Display(Name = "Назначать преподавателей")]
		AddAndRemoveInstructors = 3,
		
		[Display(Name = "Фича: антиплагиат")]
		FeatureUseAntiPlagiarism = 1001,
	}
}
