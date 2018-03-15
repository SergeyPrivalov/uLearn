using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace AntiPlagiarism.Api.Models.Parameters
{
	public abstract class ApiParameters
	{
		public NameValueCollection ToNameValueCollection(string iEnumerableValuesSeparator = ",")
		{
			/* Get all properties on the object */
			var properties = GetType().GetProperties()
				.Where(p => p.CanRead)
				.Where(p => p.GetValue(this, null) != null)
				.ToDictionary(GetPropertyNameForQueryString, p => p.GetValue(this, null));

			/* Get names for all IEnumerable properties (excl. string) */
			var iEnumerablePropertiesNames = properties
				.Where(p => !(p.Value is string) && p.Value is IEnumerable)
				.Select(p => p.Key)
				.ToList();

			/* Concat all IEnumerable properties into a comma separated string */
			foreach (var propertyName in iEnumerablePropertiesNames)
			{
				var valueType = properties[propertyName].GetType();
				var valueElemType = valueType.IsGenericType
					? valueType.GetGenericArguments()[0]
					: valueType.GetElementType();
				if (valueElemType.IsPrimitive || valueElemType == typeof(string))
				{
					var enumerable = properties[propertyName] as IEnumerable;
					properties[propertyName] = string.Join(iEnumerableValuesSeparator, enumerable.Cast<object>());
				}
			}

			var nameValueCollection = new NameValueCollection();
			foreach (var key in properties.Keys)
				nameValueCollection.Add(key, properties[key].ToString());

			return nameValueCollection;
		}

		private static string GetPropertyNameForQueryString(PropertyInfo property)
		{
			var fromQueryAttribute = property.GetCustomAttribute<FromQueryAttribute>();
			if (fromQueryAttribute != null)
				return fromQueryAttribute.Name;
			return property.Name;
		}
	}
}