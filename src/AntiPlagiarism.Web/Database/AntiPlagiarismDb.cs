using AntiPlagiarism.Web.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiPlagiarism.Web.Database
{
	public class AntiPlagiarismDb : DbContext
	{
		public AntiPlagiarismDb(DbContextOptions<AntiPlagiarismDb> options) 
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.HasDefaultSchema("antiplagiarism");
			
			modelBuilder.Entity<Client>()
				.HasIndex(c => c.Token)
				.IsUnique();
			modelBuilder.Entity<Client>()
				.HasIndex(c => new { c.Token, c.IsEnabled });

			modelBuilder.Entity<SnippetOccurence>()
				.HasIndex(c => new { c.SubmissionId, c.FirstTokenIndex })
				.IsUnique(false);

			modelBuilder.Entity<Snippet>()
				.HasIndex(c => new { c.TokensCount, c.SnippetType, c.Hash })
				.IsUnique();

			modelBuilder.Entity<SnippetStatistics>()
				.HasIndex(c => new { c.SnippetId, c.TaskId, c.ClientId })
				.IsUnique();

			var submissionEntityBuilder = modelBuilder.Entity<Submission>();
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.TaskId });
			submissionEntityBuilder.HasIndex(c => new { c.ClientId, c.TaskId, c.AuthorId });
		}

		public void MigrateToLatestVersion()
		{
			Database.Migrate();
		}

		public DbSet<Client> Clients { get; set; }
		public DbSet<Submission> Submissions { get; set; }
		public DbSet<Code> Codes { get; set; }
		public DbSet<Snippet> Snippets { get; set; }
		public DbSet<SnippetStatistics> SnippetsStatistics { get; set; }
		public DbSet<SnippetOccurence> SnippetsOccurences { get; set; }
		public DbSet<TaskStatisticsParameters> TasksStatisticsParameters { get; set; }
	}
}