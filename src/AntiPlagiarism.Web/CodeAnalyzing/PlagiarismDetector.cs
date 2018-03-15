using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api.Models;
using AntiPlagiarism.Api.Models.Results;
using AntiPlagiarism.Web.CodeAnalyzing.CSharp;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database.Models;
using AntiPlagiarism.Web.Database.Repos;
using AntiPlagiarism.Web.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using Ulearn.Common;

namespace AntiPlagiarism.Web.CodeAnalyzing
{
	public class PlagiarismDetector
	{
		private readonly ISnippetsRepo snippetsRepo;
		private readonly ISubmissionsRepo submissionsRepo;
		private readonly CodeUnitsExtractor codeUnitsExtractor;
		private readonly ILogger logger;
		private readonly AntiPlagiarismConfiguration configuration;

		public PlagiarismDetector(
			ISnippetsRepo snippetsRepo, ISubmissionsRepo submissionsRepo,
			CodeUnitsExtractor codeUnitsExtractor,
			ILogger logger,
			IOptions<AntiPlagiarismConfiguration> options)
		{
			this.snippetsRepo = snippetsRepo;
			this.submissionsRepo = submissionsRepo;
			this.codeUnitsExtractor = codeUnitsExtractor;
			this.logger = logger;
			configuration = options.Value;
		}

		public async Task<double> GetWeightAsync(Submission firstSubmission, Submission secondSubmission)
		{
			logger.Information($"Вычисляю коэффициент похожести решения #{firstSubmission.Id} и #{secondSubmission.Id}");
			var maxSnippetsCount = configuration.PlagiarismDetector.CountOfColdestSnippetsUsedToSearch;
			var snippetsOccurencesOfFirstSubmission = await snippetsRepo.GetSnippetsOccurencesForSubmissionAsync(firstSubmission, maxSnippetsCount);
			logger.Debug($"Сниппеты первого решения: [{string.Join(", ", snippetsOccurencesOfFirstSubmission)}]");
			var snippetsOccurencesOfSecondSubmission = await snippetsRepo.GetSnippetsOccurencesForSubmissionAsync(secondSubmission, maxSnippetsCount);
			logger.Debug($"Сниппеты второго решения: [{string.Join(", ", snippetsOccurencesOfSecondSubmission)}]");

			var tokensMatchedInFirstSubmission = new DefaultDictionary<SnippetType, HashSet<int>>();
			var tokensMatchedInSecondSubmission = new DefaultDictionary<SnippetType, HashSet<int>>();
			foreach (var snippetOccurence in snippetsOccurencesOfFirstSubmission)
			{
				var snippet = snippetOccurence.Snippet;
				foreach (var otherOccurence in snippetsOccurencesOfSecondSubmission.Where(o => o.SnippetId == snippet.Id))
				{
					logger.Debug($"Нашёл совпадающий сниппет в обоих решениях: {snippet}");
					for (var i = 0; i < snippet.TokensCount; i++)
					{
						tokensMatchedInFirstSubmission[snippet.SnippetType].Add(snippetOccurence.FirstTokenIndex + i);
						tokensMatchedInSecondSubmission[snippet.SnippetType].Add(otherOccurence.FirstTokenIndex + i);
					}
				}
			}
			logger.Debug("Закончил поиск совпадающих сниппетов");

			var unionLength = 0;
			var allSnippetTypes = GetAllSnippetTypes();
			foreach (var snippetType in allSnippetTypes)
			{
				if (!tokensMatchedInFirstSubmission.ContainsKey(snippetType))
					continue;
					
				unionLength += tokensMatchedInFirstSubmission[snippetType].Count;
				unionLength += tokensMatchedInSecondSubmission[snippetType].Count;
			}
			
			var totalLength = firstSubmission.TokensCount + secondSubmission.TokensCount;
			var weight = totalLength == 0 ? 0 : ((double)unionLength) / totalLength;
			logger.Information($"Совпавших токенов {unionLength}, всего токенов {totalLength}, итоговый коэфициент {weight}");
			
			/* Normalize weight */
			weight /= allSnippetTypes.Count;
			return weight;
		}

		public async Task<List<Plagiarism>> GetPlagiarismsAsync(Submission submission, SuspicionLevels suspicionLevels)
		{
			/* Dictionaries by submission id and snippet type */
			var tokensMatchedInThisSubmission = new DefaultDictionary<Tuple<int, SnippetType>, HashSet<int>>();
			var tokensMatchedInOtherSubmissions = new DefaultDictionary<Tuple<int, SnippetType>, HashSet<int>>();
		
			var maxSnippetsCount = configuration.PlagiarismDetector.CountOfColdestSnippetsUsedToSearch;
			var snippetsOccurences = await snippetsRepo.GetSnippetsOccurencesForSubmissionAsync(submission, maxSnippetsCount);
			var snippetsStatistics = await snippetsRepo.GetSnippetsStatisticsAsync(submission.ClientId, submission.TaskId, snippetsOccurences.Select(o => o.SnippetId));
			var authorsCount = await submissionsRepo.GetAuthorsCountAsync(submission.ClientId, submission.TaskId);
			var matchedSnippets = new DefaultDictionary<int, List<MatchedSnippet>>();
			foreach (var snippetOccurence in snippetsOccurences)
			{
				var otherOccurences = await snippetsRepo.GetSnippetsOccurencesAsync(
					snippetOccurence.SnippetId,
					/* Filter only snippet occurences in submissions BY THIS client, THIS task, THIS language and NOT BY THIS author */
					o => o.Submission.ClientId == submission.ClientId &&
						o.Submission.TaskId == submission.TaskId &&
						o.Submission.Language == submission.Language &&
						o.Submission.AuthorId != submission.AuthorId
				);

				var snippet = snippetOccurence.Snippet;
				var snippetType = snippet.SnippetType;

				foreach (var otherOccurence in otherOccurences)
				{
					for (var i = 0; i < snippet.TokensCount; i++)
					{
						var tokenIndexInThisSubmission = snippetOccurence.FirstTokenIndex + i;
						var tokenIndexInOtherSubmission = otherOccurence.FirstTokenIndex + i;
						tokensMatchedInThisSubmission[Tuple.Create(otherOccurence.SubmissionId, snippetType)].Add(tokenIndexInThisSubmission);
						tokensMatchedInOtherSubmissions[Tuple.Create(otherOccurence.SubmissionId, snippetType)].Add(tokenIndexInOtherSubmission);
					}

					matchedSnippets[otherOccurence.SubmissionId].Add(new MatchedSnippet
					{
						SnippetType = snippetType,
						TokensCount = snippet.TokensCount,
						OriginalSubmissionFirstTokenIndex = snippetOccurence.FirstTokenIndex,
						PlagiarismSubmissionFirstTokenIndex = otherOccurence.FirstTokenIndex,
						SnippetFrequency = GetSnippetFrequency(snippetsStatistics[snippet.Id], authorsCount),
					});
				}
			}

			var plagiateSubmissionIds = tokensMatchedInOtherSubmissions.Keys.Select(tuple => tuple.Item1).ToList();
			var plagiateSubmissions = await submissionsRepo.GetSubmissionsByIdsAsync(plagiateSubmissionIds);

			var plagiarisms = new List<Plagiarism>();
			
			var allSnippetTypes = GetAllSnippetTypes();
			var thisSubmissionLength = submission.TokensCount;
			foreach (var plagiarismSubmission in plagiateSubmissions)
			{
				var unionLength = 0;
				foreach (var snippetType in allSnippetTypes)
				{
					var submissionIdWithSnippetType = Tuple.Create(plagiarismSubmission.Id, snippetType);
					if (!tokensMatchedInThisSubmission.ContainsKey(submissionIdWithSnippetType))
						continue;
					
					unionLength += tokensMatchedInThisSubmission[submissionIdWithSnippetType].Count;
					unionLength += tokensMatchedInOtherSubmissions[submissionIdWithSnippetType].Count;
				}

				var plagiateSubmissionLength = plagiarismSubmission.TokensCount;
				var totalLength = thisSubmissionLength + plagiateSubmissionLength;
				var weight = ((double)unionLength) / totalLength;
				/* Normalize weight */
				weight /= allSnippetTypes.Count;

				if (weight < suspicionLevels.FaintSuspicion)
					continue;
				
				plagiarisms.Add(BuildPlagiarismInfo(plagiarismSubmission, weight, matchedSnippets[plagiarismSubmission.Id]));
			}

			return plagiarisms;
		}

		public List<TokenPosition> GetNeededTokensPositions(string program)
		{
			return GetNeededTokensPositions(codeUnitsExtractor.Extract(program));
		}

		public static List<TokenPosition> GetNeededTokensPositions(IEnumerable<CodeUnit> codeUnits)
		{
			return codeUnits.SelectMany(u => u.Tokens.Enumerate(start: u.FirstTokenIndex)).Select(t => new TokenPosition
			{
				TokenIndex = t.Index,
				StartPosition = t.Item.SpanStart,
				Length = t.Item.Span.Length,
			}).ToList();
		}

		private Plagiarism BuildPlagiarismInfo(Submission submission, double weight, List<MatchedSnippet> matchedSnippets)
		{
			var codeUnits = codeUnitsExtractor.Extract(submission.ProgramText);
			return new Plagiarism
			{
				SubmissionInfo = submission.GetSubmissionInfoForApi(),
				Weight = weight,
				AnalyzedCodeUnits = codeUnits.Select(
					u => new AnalyzedCodeUnit
					{
						Name = u.Path.ToString(),
						FirstTokenIndex = u.FirstTokenIndex,
						TokensCount = u.Tokens.Count,
					}).ToList(),
				TokensPositions = GetNeededTokensPositions(codeUnits),
				MatchedSnippets = matchedSnippets, 
			};
		}

		private static List<SnippetType> GetAllSnippetTypes()
		{
			return Enum.GetValues(typeof(SnippetType)).Cast<SnippetType>().ToList();
		}
		
		private static double GetSnippetFrequency(SnippetStatistics snippet, int authorsCount)
		{
			return (double) snippet.AuthorsCount / (authorsCount == 0 ? 1 : authorsCount);
		}
	}
}