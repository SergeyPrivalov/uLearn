using System;
using System.Runtime.Serialization;

namespace AntiPlagiarism.Api
{
	public class AntiPlagiarismClientException : Exception
	{
		public AntiPlagiarismClientException()
		{
		}

		protected AntiPlagiarismClientException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		public AntiPlagiarismClientException(string message)
			: base(message)
		{
		}

		public AntiPlagiarismClientException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}