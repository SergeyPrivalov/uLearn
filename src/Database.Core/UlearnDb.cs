using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Database.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Database
{
	public class UlearnDb : IdentityDbContext<ApplicationUser>
	{
		public UlearnDb(DbContextOptions<UlearnDb> options)
			: base(options)
		{
		}
		
		public void MigrateToLatestVersion()
		{
			Database.Migrate();
		}

		public Task CreateInitialDataAsync(InitialDataCreator creator)
		{
			return creator.CreateInitialDataAsync();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
			
			/* IdentityUser.Id is guid in ASP.NET Core, so we can limit it by 64 chars.
			   If we will not do it, foreign keys to AspNetUsers.Id will fail in ASP.NET Core
			 */
			modelBuilder.Entity<ApplicationUser>(b => { b.Property(u => u.Id).HasMaxLength(64); });

			/* Customize the ASP.NET Identity model and override the defaults if needed.
			 * See https://docs.microsoft.com/en-us/aspnet/core/migration/1x-to-2x/identity-2x#add-identityuser-poco-navigation-properties
			 * for details */
			
			modelBuilder.Entity<ApplicationUser>()
				.HasMany(e => e.Claims)
				.WithOne()
				.HasForeignKey(e => e.UserId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ApplicationUser>()
				.HasMany(e => e.Logins)
				.WithOne()
				.HasForeignKey(e => e.UserId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ApplicationUser>()
				.HasMany(e => e.Roles)
				.WithOne()
				.HasForeignKey(e => e.UserId)
				.IsRequired()
				.OnDelete(DeleteBehavior.Cascade);

			/* "If you don't want to expose a DbSet for one or more entities in the hierarchy, you can use the Fluent API to ensure they are included in the model".
			 * See https://stackoverflow.com/questions/37398141/ef7-migrations-the-corresponding-clr-type-for-entity-type-is-not-instantiab for details
			 */
			modelBuilder.Entity<MailNotificationTransport>();
			modelBuilder.Entity<TelegramNotificationTransport>();
			modelBuilder.Entity<FeedNotificationTransport>();
			
			
			modelBuilder.Entity<CommentLike>()
				.HasOne(x => x.Comment)
				.WithMany(x => x.Likes)
				.HasForeignKey(x => x.CommentId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<GroupMember>()
				.HasOne(m => m.Group)
				.WithMany(g => g.Members)
				.HasForeignKey(m => m.GroupId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Like>()
				.HasOne(l => l.Submission)
				.WithMany(s => s.Likes)
				.HasForeignKey(l => l.SubmissionId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<NotificationDelivery>()
				.HasOne(d => d.Notification)
				.WithMany(n => n.Deliveries)
				.HasForeignKey(d => d.NotificationId)
				.OnDelete(DeleteBehavior.Restrict);

			CancelCascaseDeleting<ExerciseCodeReview, ApplicationUser>(modelBuilder, c => c.Author, c => c.AuthorId);

			CancelCascaseDeleting<UserExerciseSubmission, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);
			CancelCascaseDeleting<ManualExerciseChecking, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);

			CancelCascaseDeleting<Certificate, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);
			CancelCascaseDeleting<Certificate, ApplicationUser>(modelBuilder, c => c.Instructor, c => c.InstructorId);

			CancelCascaseDeleting<AdditionalScore, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);
			CancelCascaseDeleting<AdditionalScore, ApplicationUser>(modelBuilder, c => c.Instructor, c => c.InstructorId);

			CancelCascaseDeleting<GraderClient, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);

			CancelCascaseDeleting<Notification, ApplicationUser>(modelBuilder, c => c.InitiatedBy, c => c.InitiatedById);
			CancelCascaseDeleting<AddedInstructorNotification, ApplicationUser>(modelBuilder, c => c.AddedUser, c => c.AddedUserId);
			CancelCascaseDeleting<LikedYourCommentNotification, ApplicationUser>(modelBuilder, c => c.LikedUser, c => c.LikedUserId);
			CancelCascaseDeleting<JoinedToYourGroupNotification, ApplicationUser>(modelBuilder, c => c.JoinedUser, c => c.JoinedUserId);
			CancelCascaseDeleting<JoinedToYourGroupNotification, Group>(modelBuilder, c => c.Group, c => c.GroupId);
			CancelCascaseDeleting<GrantedAccessToGroupNotification, GroupAccess>(modelBuilder, c => c.Access, c => c.AccessId);
			CancelCascaseDeleting<RevokedAccessToGroupNotification, GroupAccess>(modelBuilder, c => c.Access, c => c.AccessId);
			CancelCascaseDeleting<GroupMemberHasBeenRemovedNotification, Group>(modelBuilder, c => c.Group, c => c.GroupId);
			CancelCascaseDeleting<GroupMemberHasBeenRemovedNotification, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);
			CancelCascaseDeleting<CreatedGroupNotification, Group>(modelBuilder, c => c.Group, c => c.GroupId);
			CancelCascaseDeleting<PassedManualExerciseCheckingNotification, ManualExerciseChecking>(modelBuilder, c => c.Checking, c => c.CheckingId);
			CancelCascaseDeleting<PassedManualQuizCheckingNotification, ManualQuizChecking>(modelBuilder, c => c.Checking, c => c.CheckingId);
			CancelCascaseDeleting<ReceivedAdditionalScoreNotification, AdditionalScore>(modelBuilder, c => c.Score, c => c.ScoreId);

			CancelCascaseDeleting<NewCommentNotification, Comment>(modelBuilder, c => c.Comment, c => c.CommentId);
			CancelCascaseDeleting<LikedYourCommentNotification, Comment>(modelBuilder, c => c.Comment, c => c.CommentId);
			CancelCascaseDeleting<RepliedToYourCommentNotification, Comment>(modelBuilder, c => c.Comment, c => c.CommentId);
			CancelCascaseDeleting<RepliedToYourCommentNotification, Comment>(modelBuilder, c => c.ParentComment, c => c.ParentCommentId);
			
			CancelCascaseDeleting<UploadedPackageNotification, CourseVersion>(modelBuilder, c => c.CourseVersion, c => c.CourseVersionId);
			CancelCascaseDeleting<PublishedPackageNotification, CourseVersion>(modelBuilder, c => c.CourseVersion, c => c.CourseVersionId);

			CancelCascaseDeleting<CourseExportedToStepikNotification, StepikExportProcess>(modelBuilder, c => c.Process, c => c.ProcessId);
			
			CancelCascaseDeleting<XQueueWatcher, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);

			CancelCascaseDeleting<StepikExportProcess, ApplicationUser>(modelBuilder, c => c.Owner, c => c.OwnerId);

			CancelCascaseDeleting<NotificationTransport, ApplicationUser>(modelBuilder, c => c.User, c => c.UserId);

			CancelCascaseDeleting<LabelOnGroup, Group>(modelBuilder, c => c.Group, c => c.GroupId);
			CancelCascaseDeleting<GroupLabel, ApplicationUser>(modelBuilder, c => c.Owner, c => c.OwnerId);
			CancelCascaseDeleting<LabelOnGroup, GroupLabel>(modelBuilder, c => c.Label, c => c.LabelId);
			
			CancelCascaseDeleting<SystemAccess, ApplicationUser>(modelBuilder, c => c.GrantedBy, c => c.GrantedById);


			CreateIndexes(modelBuilder);
		}

		private static void CancelCascaseDeleting<T1, T2>(ModelBuilder modelBuilder, Expression<Func<T1, T2>> oneWay, Expression<Func<T1, object>> secondWay, DeleteBehavior deleteBehavior=DeleteBehavior.Restrict)
			where T1 : class
			where T2 : class
		{
			modelBuilder.Entity<T1>()
				.HasOne(oneWay)
				.WithMany()
				.HasForeignKey(secondWay)
				.OnDelete(deleteBehavior);
		}
		
		private void CreateIndexes(ModelBuilder modelBuilder)
		{
			AddIndex<AdditionalScore>(modelBuilder, c => new { c.CourseId, c.UserId });
			AddIndex<AdditionalScore>(modelBuilder, c => new { c.CourseId, c.UserId, c.UnitId, c.ScoringGroupId }, isUnique: true);
			AddIndex<AdditionalScore>(modelBuilder, c => c.UnitId);

			AddIndex<ApplicationUser>(modelBuilder, c => c.TelegramChatId);

			AddIndex<Certificate>(modelBuilder, c => c.TemplateId);
			AddIndex<Certificate>(modelBuilder, c => c.UserId);

			AddIndex<CertificateTemplate>(modelBuilder, c => c.CourseId);

			AddIndex<Comment>(modelBuilder, c => c.SlideId);
			AddIndex<Comment>(modelBuilder, c => new { c.AuthorId, c.PublishTime });

			AddIndex<CommentLike>(modelBuilder, c => new { c.UserId, c.CommentId }, isUnique: true);
			AddIndex<CommentLike>(modelBuilder, c => c.CommentId);

			AddIndex<CourseAccess>(modelBuilder, c => c.CourseId);
			AddIndex<CourseAccess>(modelBuilder, c => new { c.CourseId, c.IsEnabled });
			AddIndex<CourseAccess>(modelBuilder, c => new { c.CourseId, c.UserId, c.IsEnabled });
			AddIndex<CourseAccess>(modelBuilder, c => c.GrantTime);

			AddIndex<CourseVersion>(modelBuilder, c => new { c.CourseId, c.PublishTime });
			AddIndex<CourseVersion>(modelBuilder, c => new { c.CourseId, c.LoadingTime });

			AddIndex<EnabledAdditionalScoringGroup>(modelBuilder, c => c.GroupId);
			
			AddIndex<ExerciseCodeReview>(modelBuilder, c => c.ExerciseCheckingId);

			AddIndex<FeedViewTimestamp>(modelBuilder, c => c.UserId);
			AddIndex<FeedViewTimestamp>(modelBuilder, c => c.Timestamp);
			AddIndex<FeedViewTimestamp>(modelBuilder, c => new { c.UserId, c.TransportId });

			AddIndex<Group>(modelBuilder, c => c.CourseId);
			AddIndex<Group>(modelBuilder, c => c.OwnerId);
			AddIndex<Group>(modelBuilder, c => c.InviteHash);

			AddIndex<GroupAccess>(modelBuilder, c => c.GroupId);
			AddIndex<GroupAccess>(modelBuilder, c => new { c.GroupId, c.IsEnabled });
			AddIndex<GroupAccess>(modelBuilder, c => new { c.GroupId, c.UserId, c.IsEnabled });
			AddIndex<GroupAccess>(modelBuilder, c => c.UserId);
			AddIndex<GroupAccess>(modelBuilder, c => c.GrantTime);

			AddIndex<GroupLabel>(modelBuilder, c => c.OwnerId);
			AddIndex<GroupLabel>(modelBuilder, c => new { c.OwnerId, c.IsDeleted});

			AddIndex<LabelOnGroup>(modelBuilder, c => c.GroupId);
			AddIndex<LabelOnGroup>(modelBuilder, c => c.LabelId);
			AddIndex<LabelOnGroup>(modelBuilder, c => new { c.GroupId, c.LabelId }, isUnique: true);

			AddIndex<GroupMember>(modelBuilder, c => c.GroupId);
			AddIndex<GroupMember>(modelBuilder, c => c.UserId);

			AddIndex<Like>(modelBuilder, c => c.SubmissionId);
			AddIndex<Like>(modelBuilder, c => new { c.UserId, c.SubmissionId });

			AddIndex<LtiConsumer>(modelBuilder, c => c.Key);

			AddIndex<LtiSlideRequest>(modelBuilder, c => new { c.SlideId, c.UserId });

			AddIndex<NotificationTransport>(modelBuilder, c => c.UserId);
			AddIndex<NotificationTransport>(modelBuilder, c => new { c.UserId, c.IsDeleted });

			AddIndex<NotificationTransportSettings>(modelBuilder, c => c.NotificationTransportId);
			AddIndex<NotificationTransportSettings>(modelBuilder, c => c.CourseId);
			AddIndex<NotificationTransportSettings>(modelBuilder, c => c.NotificationType);
			AddIndex<NotificationTransportSettings>(modelBuilder, c => new { c.CourseId, c.NotificationType });

			AddIndex<NotificationDelivery>(modelBuilder, c => c.CreateTime);
			AddIndex<NotificationDelivery>(modelBuilder, c => c.NextTryTime);
			AddIndex<NotificationDelivery>(modelBuilder, c => new { c.NotificationId, c.NotificationTransportId });

			AddIndex<Notification>(modelBuilder, c => c.CourseId);
			AddIndex<Notification>(modelBuilder, c => c.CreateTime);
			AddIndex<Notification>(modelBuilder, c => c.AreDeliveriesCreated);

			AddIndex<QuizVersion>(modelBuilder, c => c.SlideId);
			AddIndex<QuizVersion>(modelBuilder, c => new { c.SlideId, c.LoadingTime });
		
			AddIndex<ManualExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId });
			AddIndex<ManualExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId, c.ProhibitFurtherManualCheckings });
			AddIndex<ManualExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.Timestamp });
			AddIndex<ManualExerciseChecking>(modelBuilder, c => new { c.CourseId, c.UserId });
			AddIndex<AutomaticExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId });
			AddIndex<AutomaticExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId });
			AddIndex<AutomaticExerciseChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.Timestamp });
			AddIndex<AutomaticExerciseChecking>(modelBuilder, c => new { c.CourseId, c.UserId });
			AddIndex<ManualQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId });
			AddIndex<ManualQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId });
			AddIndex<ManualQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.Timestamp });
			AddIndex<ManualQuizChecking>(modelBuilder, c => new { c.CourseId, c.UserId });
			AddIndex<AutomaticQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId });
			AddIndex<AutomaticQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId });
			AddIndex<AutomaticQuizChecking>(modelBuilder, c => new { c.CourseId, c.SlideId, c.Timestamp });
			AddIndex<AutomaticQuizChecking>(modelBuilder, c => new { c.CourseId, c.UserId });

			AddIndex<AutomaticExerciseChecking>(modelBuilder, c => c.IsRightAnswer);

			AddIndex<SlideHint>(modelBuilder, c => new { c.SlideId, c.HintId, c.UserId, c.IsHintHelped });

			AddIndex<SlideRate>(modelBuilder, c => new { c.SlideId, c.UserId });

			AddIndex<StepikAccessToken>(modelBuilder, c => c.AddedTime);

			AddIndex<StepikExportProcess>(modelBuilder, c => c.OwnerId);

			AddIndex<StepikExportSlideAndStepMap>(modelBuilder, c => c.UlearnCourseId);
			AddIndex<StepikExportSlideAndStepMap>(modelBuilder, c => new { c.UlearnCourseId, c.StepikCourseId });
			AddIndex<StepikExportSlideAndStepMap>(modelBuilder, c => new { c.UlearnCourseId, c.SlideId });

			AddIndex<SystemAccess>(modelBuilder, c => c.UserId);
			AddIndex<SystemAccess>(modelBuilder, c => c.GrantTime);
			AddIndex<SystemAccess>(modelBuilder, c => c.IsEnabled);
			AddIndex<SystemAccess>(modelBuilder, c => new { c.UserId, c.IsEnabled });

			AddIndex<UnitAppearance>(modelBuilder, c => new { c.CourseId, c.PublishTime });

			AddIndex<UserExerciseSubmission>(modelBuilder, c => c.AutomaticCheckingIsRightAnswer);
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.CourseId, c.SlideId });
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId });
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.CourseId, c.SlideId, c.Timestamp });
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.CourseId, c.AutomaticCheckingIsRightAnswer });
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.CourseId, c.SlideId, c.AutomaticCheckingIsRightAnswer });
			AddIndex<UserExerciseSubmission>(modelBuilder, c => new { c.AntiPlagiarismSubmissionId });

			AddIndex<UserQuiz>(modelBuilder, c => new { c.SlideId, c.Timestamp });
			AddIndex<UserQuiz>(modelBuilder, c => new { c.UserId, c.SlideId, c.isDropped, c.QuizId, c.ItemId });
			
			AddIndex<Visit>(modelBuilder, c => new { c.SlideId, c.UserId });
			AddIndex<Visit>(modelBuilder, c => new { c.CourseId, c.SlideId, c.UserId });
			AddIndex<Visit>(modelBuilder, c => new { c.SlideId, c.Timestamp });
		}

		private void AddIndex<TEntity>(ModelBuilder modelBuilder, Expression<Func<TEntity, object>> indexFunction, bool isUnique=false) where TEntity : class
		{
			modelBuilder.Entity<TEntity>().HasIndex(indexFunction).IsUnique(isUnique);
		}

		/* Construct easy understandable message on DbEntityValidationException */
		public override int SaveChanges()
		{
			ValidateChanges();
			return base.SaveChanges();
		}

		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
		{
			ValidateChanges();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		private void ValidateChanges()
		{
			var entities = from e in ChangeTracker.Entries()
				where e.State == EntityState.Added
					|| e.State == EntityState.Modified
				select e.Entity;
			foreach (var entity in entities)
			{
				var validationContext = new ValidationContext(entity);
				Validator.ValidateObject(entity, validationContext);
			}
		}

		public DbSet<UserQuestion> UserQuestions { get; set; }
		public DbSet<SlideRate> SlideRates { get; set; }
		public DbSet<Visit> Visits { get; set; }
		public DbSet<SlideHint> Hints { get; set; }
		public DbSet<Like> SolutionLikes { get; set; }
		public DbSet<UserQuiz> UserQuizzes { get; set; }
		public DbSet<UnitAppearance> UnitAppearances { get; set; }
		public DbSet<TextBlob> Texts { get; set; }
		public DbSet<LtiConsumer> Consumers { get; set; }
		public DbSet<LtiSlideRequest> LtiRequests { get; set; }
		public DbSet<RestoreRequest> RestoreRequests { get; set; }
		public DbSet<UserRole> UserRoles { get; set; }

		public DbSet<Comment> Comments { get; set; }
		public DbSet<CommentLike> CommentLikes { get; set; }
		public DbSet<CommentsPolicy> CommentsPolicies { get; set; }

		public DbSet<QuizVersion> QuizVersions { get; set; }
		public DbSet<CourseVersion> CourseVersions { get; set; }

		public DbSet<ManualExerciseChecking> ManualExerciseCheckings { get; set; }
		public DbSet<AutomaticExerciseChecking> AutomaticExerciseCheckings { get; set; }
		public DbSet<ManualQuizChecking> ManualQuizCheckings { get; set; }
		public DbSet<AutomaticQuizChecking> AutomaticQuizCheckings { get; set; }
		public DbSet<UserExerciseSubmission> UserExerciseSubmissions { get; set; }
		public DbSet<ExerciseCodeReview> ExerciseCodeReviews { get; set; }

		public DbSet<Group> Groups { get; set; }
		public DbSet<GroupMember> GroupMembers { get; set; }
		public DbSet<GroupLabel> GroupLabels { get; set; }
		public DbSet<LabelOnGroup> LabelsOnGroups { get; set; }
		public DbSet<GroupAccess> GroupAccesses { get; set; }

		public DbSet<CertificateTemplate> CertificateTemplates { get; set; }
		public DbSet<Certificate> Certificates { get; set; }

		public DbSet<AdditionalScore> AdditionalScores { get; set; }
		public DbSet<EnabledAdditionalScoringGroup> EnabledAdditionalScoringGroups { get; set; }

		public DbSet<GraderClient> GraderClients { get; set; }
		public DbSet<ExerciseSolutionByGrader> ExerciseSolutionsByGrader { get; set; }

		public DbSet<NotificationTransport> NotificationTransports { get; set; }
		public DbSet<NotificationTransportSettings> NotificationTransportSettings { get; set; }
		public DbSet<NotificationDelivery> NotificationDeliveries { get; set; }
		public DbSet<Notification> Notifications { get; set; }

		public DbSet<XQueueWatcher> XQueueWatchers { get; set; }
		public DbSet<XQueueExerciseSubmission> XQueueExerciseSubmissions { get; set; }

		public DbSet<FeedViewTimestamp> FeedViewTimestamps { get; set; }

		public DbSet<StepikAccessToken> StepikAccessTokens { get; set; }
		public DbSet<StepikExportProcess> StepikExportProcesses { get; set; }
		public DbSet<StepikExportSlideAndStepMap> StepikExportSlideAndStepMaps { get; set; }

		public DbSet<CourseAccess> CourseAccesses { get; set; }
		public DbSet<SystemAccess> SystemAccesses { get; set; }
	}
}
 