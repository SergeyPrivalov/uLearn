using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ulearn.Common.Extensions
{
	public static class DbSetExtensions
	{
		public static EntityEntry<TEntity> AddOrUpdate<TEntity>(this DbContext dbContext, TEntity entity, Expression<Func<TEntity, bool>> findFunction) where TEntity : class
		{
			var dbSet = dbContext.Set<TEntity>();
			var exists = dbSet.Any(findFunction);
			
			if (!exists)
				return dbContext.Add(entity);

			var dbValue = dbSet.FirstOrDefault(findFunction);
			var entry = dbContext.Entry(dbValue);
			entry.CurrentValues.SetValues(entity);
			entry.State = EntityState.Modified;
			return entry;
		}
	}
}