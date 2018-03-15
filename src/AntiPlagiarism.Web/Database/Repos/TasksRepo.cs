using System;
using System.Threading.Tasks;
using AntiPlagiarism.Web.Database.Models;
using AntiPlagiarism.Web.Extensions;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Web.Database.Repos
{
	public interface ITasksRepo
	{
		Task<TaskStatisticsParameters> FindTaskStatisticsParametersAsync(Guid taskId);
		Task SaveTaskStatisticsParametersAsync(TaskStatisticsParameters parameters);
	}
	
	public class TasksRepo : ITasksRepo
	{
		private readonly AntiPlagiarismDb db;

		public TasksRepo(AntiPlagiarismDb db)
		{
			this.db = db;
		}

		public Task<TaskStatisticsParameters> FindTaskStatisticsParametersAsync(Guid taskId)
		{
			return db.TasksStatisticsParameters.FindAsync(taskId);
		}

		public async Task SaveTaskStatisticsParametersAsync(TaskStatisticsParameters parameters)
		{
			using (var transaction = db.Database.BeginTransaction())
			{
				db.AddOrUpdate(parameters, p => p.TaskId == parameters.TaskId);
				await db.SaveChangesAsync();
				transaction.Commit();	
			}
		}
	}
}