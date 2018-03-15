using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using LtiLibrary.NetCore.Lti.v1;
using Newtonsoft.Json;
using Ulearn.Common;
using Ulearn.Common.Extensions;

namespace Database.Repos
{
	public class LtiRequestsRepo
	{
		private readonly UlearnDb db;
		private readonly JsonSerializer serializer;

		public LtiRequestsRepo(UlearnDb db)
		{
			this.db = db;
			serializer = new JsonSerializer();
		}

		public async Task Update(string userId, Guid slideId, string ltiRequestJson)
		{
			await FuncUtils.TrySeveralTimesAsync(() => TryUpdate(userId, slideId, ltiRequestJson), 3);
		}

		private async Task TryUpdate(string userId, Guid slideId, string ltiRequestJson)
		{
			var ltiRequestModel = FindElement(userId, slideId);

			if (ltiRequestModel == null)
			{
				ltiRequestModel = new LtiSlideRequest
				{
					UserId = userId,
					SlideId = slideId,
					Request = ltiRequestJson
				};
			}
			else
				ltiRequestModel.Request = ltiRequestJson;

			db.AddOrUpdate(ltiRequestModel, r => r.RequestId == ltiRequestModel.RequestId);
			await db.SaveChangesAsync();
		}

		public LtiRequest Find(string userId, Guid slideId)
		{
			var ltiRequestModel = FindElement(userId, slideId);
			if (ltiRequestModel == null)
				return null;

			return serializer.Deserialize<LtiRequest>(new JsonTextReader(new StringReader(ltiRequestModel.Request)));
		}

		private LtiSlideRequest FindElement(string userId, Guid slideId)
		{
			return db.LtiRequests.FirstOrDefault(request => request.UserId == userId && request.SlideId == slideId);
		}
	}
}