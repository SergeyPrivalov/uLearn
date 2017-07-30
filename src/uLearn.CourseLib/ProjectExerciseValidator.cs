using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.VisualBasic.FileIO;
using RunCsJob;
using RunCsJob.Api;
using uLearn.Extensions;
using uLearn.Model.Blocks;
using SearchOption = Microsoft.VisualBasic.FileIO.SearchOption;

namespace uLearn
{
	public class ProjectExerciseValidator : BaseValidator
	{
		private readonly ProjectExerciseBlock ex;
		private readonly ExerciseSlide slide;
		private readonly SandboxRunnerSettings settings;

		public ProjectExerciseValidator(BaseValidator baseValidator, SandboxRunnerSettings settings, ExerciseSlide slide, ProjectExerciseBlock exercise)
			: base(baseValidator)
		{
			this.settings = settings;
			this.slide = slide;
			ex = exercise;
		}

		public void ValidateExercises()
		{
			var projItems = new Project(ex.CsprojFile.FullName, null, null, new ProjectCollection()).Items;
			var compile = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Compile").Select(i => i.UnevaluatedInclude));
			var content = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Content").Select(i => i.UnevaluatedInclude));
			Console.WriteLine($@"Method(ValidateExercises) at bootstrap
Csproj Compiles:
{compile}

Csproj Contents:
{content}


");
			if (ReportErrorIfExerciseFolderMissesRequiredFiles())
				return;

			ReportWarningIfWrongAnswersAreSolutionsOrNotOk();

			if (!ReportWarningIfExerciseFolderDoesntContainSolutionFile())
				ReportErrorIfSolutionNotBuildingOrNotPassesTests();

			ReportErrorIfStudentsZipHasErrors();

			ReportErrorIfInitialCodeIsSolutionOrVerdictNotOk();
		}

		private bool ReportWarningIfExerciseFolderDoesntContainSolutionFile()
		{
			if (ExerciseDirectoryContainsSolutionFile())
				return false;
			ReportSlideWarning(slide, $"Exercise directory doesn't contain {ex.CorrectSolutionFileName}");
			return true;
		}

		private bool ReportErrorIfExerciseFolderMissesRequiredFiles()
		{
			var exerciseFilesRelativePaths = FileSystem.GetFiles(ex.ExerciseFolder.FullName, SearchOption.SearchAllSubDirectories)
				.Select(path => new FileInfo(path).GetRelativePath(ex.ExerciseFolder.FullName))
				.ToList();

			return ReportErrorIfMissingCsproj() | ReportErrorIfMissingUserCodeFile();

			bool ReportErrorIfMissingUserCodeFile() => ReportErrorIfMissingFile(ex.UserCodeFilePath);
			bool ReportErrorIfMissingCsproj() => ReportErrorIfMissingFile(ex.CsprojFileName);

			bool ReportErrorIfMissingFile(string path)
			{
				if (exerciseFilesRelativePaths.Any(p => p.Equals(path, StringComparison.InvariantCultureIgnoreCase)))
					return false;
				ReportSlideError(slide, $"Exercise folder ({ex.ExerciseFolder.Name}) doesn't contain ({path})");
				return true;
			}
		}

		private bool ExerciseDirectoryContainsSolutionFile()
			=> ex.UserCodeFileParentDirectory.GetFiles().Any(f => f.Name.Equals(ex.CorrectSolutionFileName, StringComparison.InvariantCultureIgnoreCase));

		private void ReportErrorIfSolutionNotBuildingOrNotPassesTests()
		{
			var solutionCode = ex.CorrectSolutionFile.ContentAsUtf8();
			var submission = ex.CreateSubmission(ex.CsprojFileName, solutionCode);
			var result = SandboxRunner.Run(submission, new SandboxRunnerSettings());

			var projItems = new Project(ex.CsprojFile.FullName, null, null, new ProjectCollection()).Items;
			var compile = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Compile").Select(i => i.UnevaluatedInclude));
			var content = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Content").Select(i => i.UnevaluatedInclude));
			Console.WriteLine($@"Method(ReportErrorIfSolutionNotBuildingOrNotPassesTests)
after SandboxRunner.Run with solution code: 
Csproj Compiles:
{compile}

Csproj Contents:
{content}


");
			if (VerdictIsNotOk(result))
				ReportError($"Correct solution file {ex.CorrectSolutionFileName} verdict is not OK. RunResult = {result}");

			if (!IsSolution(result))
				ReportError($"Correct solution file {ex.CorrectSolutionFileName} is not solution. RunResult = {result}");
		}

		private void ReportWarningIfWrongAnswersAreSolutionsOrNotOk()
		{
			var filesWithWrongAnswer = FileSystem.GetFiles(ex.ExerciseFolder.FullName, SearchOption.SearchAllSubDirectories)
				.Select(name => new FileInfo(name))
				.Where(f => ex.IsWrongAnswer(f.Name));

			foreach (var waFile in filesWithWrongAnswer)
			{
				var result = SandboxRunner.Run(ex.CreateSubmission(waFile.Name, waFile.ContentAsUtf8()));

				var projItems = new Project(ex.CsprojFile.FullName, null, null, new ProjectCollection()).Items;
				var compile = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Compile").Select(i => i.UnevaluatedInclude));
				var content = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Content").Select(i => i.UnevaluatedInclude));
				Console.WriteLine($@"Method(ReportWarningIfWrongAnswersAreSolutionsOrNotOk)
after SandboxRunner.Run({waFile.Name}): 
Csproj Compiles:
{compile}

Csproj Contents:
{content}


");
				ReportWarningIfWrongAnswerVerdictIsNotOk(waFile.Name, result);
				ReportWarningIfWrongAnswerIsSolution(waFile.Name, result);
			}
		}

		private void ReportWarningIfWrongAnswerVerdictIsNotOk(string waFileName, RunningResults waResult)
		{
			if (VerdictIsNotOk(waResult))
				ReportSlideWarning(slide, $"Code verdict of file with wrong answer ({waFileName}) is not OK. RunResult = " + waResult);
		}

		private void ReportWarningIfWrongAnswerIsSolution(string waFileName, RunningResults waResult)
		{
			if (IsSolution(waResult))
				ReportSlideWarning(slide, $"Code of file with wrong answer ({waFileName}) is solution!");
		}

		private void ReportErrorIfInitialCodeIsSolutionOrVerdictNotOk()
		{
			var initialCode = ex.UserCodeFile.ContentAsUtf8();
			var submission = ex.CreateSubmission(ex.CsprojFileName, initialCode);
			var result = SandboxRunner.Run(submission);

			if (ex.StudentZipIsBuildable)
				ReportErrorIfInitialCodeVerdictIsNotOk(result);

			ReportErrorIfInitialCodeIsSolution(result);
		}

		private void ReportErrorIfInitialCodeVerdictIsNotOk(RunningResults result)
		{
			if (VerdictIsNotOk(result))
				ReportSlideError(slide, "Exercise initial code verdict is not OK. RunResult = " + result);
		}

		private void ReportErrorIfInitialCodeIsSolution(RunningResults result)
		{
			if (IsSolution(result))
				ReportSlideError(slide, "Exercise initial code (available to students) is solution!");
		}

		private void ReportErrorIfStudentsZipHasErrors()
		{
			var tempExFolder = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExerciseFolder_From_StudentZip"));
			Utils.UnpackZip(ex.StudentsZip.Content(), tempExFolder.FullName);
			try
			{
				ReportErrorIfStudentsZipHasWrongAnswerOrSolutionFiles(tempExFolder);
				ReportErrorIfCsprojHasUserCodeOfNotCompileType(tempExFolder);
				ReportErrorIfCsprojHasWrongAnswerOrSolutionItems(tempExFolder);

				if (!ex.StudentZipIsBuildable)
					return;

				var buildResult = MsBuildRunner.BuildProject(settings.MsBuildSettings, ex.CsprojFile.Name, tempExFolder);
				ReportErrorIfStudentsZipNotBuilding(buildResult);
			}
			finally
			{
				tempExFolder.Delete(true);
			}
		}

		private void ReportErrorIfStudentsZipNotBuilding(MSbuildResult res)
		{
			if (!res.Success)
				ReportSlideError(slide, ex.CsprojFileName + " not building! " + res);
		}

		private void ReportErrorIfStudentsZipHasWrongAnswerOrSolutionFiles(DirectoryInfo unpackedZipDir)
		{
			var wrongAnswersOrSolution = GetOrderedFileNames(unpackedZipDir.GetRelativePathsOfFiles(), ProjectExerciseBlock.IsAnyWrongAnswerOrAnySolution);

			if (wrongAnswersOrSolution.Any())
				ReportSlideError(slide, $"Student zip exercise directory has 'wrong answer' and/or solution files ({string.Join(", ", wrongAnswersOrSolution)})");
		}

		private string[] GetOrderedFileNames(IEnumerable<string> names, Func<string, bool> predicate) => names.Where(predicate).OrderBy(n => n).ToArray();

		private void ReportErrorIfCsprojHasUserCodeOfNotCompileType(DirectoryInfo unpackedZipDir)
		{
			var csproj = unpackedZipDir.GetFiles(ex.CsprojFileName).Single();

			var projItems = new Project(csproj.FullName, null, null, new ProjectCollection()).Items;
			var compile = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Compile").Select(i => i.UnevaluatedInclude));
			var content = string.Join("\r\n", projItems.Where(pi => pi.ItemType == "Content").Select(i => i.UnevaluatedInclude));
			Console.WriteLine($@"Method(ReportErrorIfCsprojHasUserCodeOfNotCompileType)
after unpacking to {unpackedZipDir}
contained files: {string.Join("\r\n", FileSystem.GetFiles(unpackedZipDir.FullName, SearchOption.SearchAllSubDirectories).Select(path => new FileInfo(path)).Select(f => f.GetRelativePath(unpackedZipDir.FullName)))}
UNPACKED Csproj Compiles:
{compile}

UNPACKED Csproj Contents:
{content}


");
			var userCode = new Project(csproj.FullName, null, null, new ProjectCollection()).Items
				.Single(i => i.UnevaluatedInclude.Equals(ex.UserCodeFilePath, StringComparison.InvariantCultureIgnoreCase));

			if (!userCode.ItemType.Equals("Compile", StringComparison.InvariantCultureIgnoreCase))
				ReportSlideError(slide, $"Student's csproj has user code item ({userCode.UnevaluatedInclude}) of not compile type");
		}

		private void ReportErrorIfCsprojHasWrongAnswerOrSolutionItems(DirectoryInfo unpackedZipDir)
		{
			var csprojFile = unpackedZipDir.GetFiles(ex.CsprojFileName).Single();
			var csProj = new Project(csprojFile.FullName, null, null, new ProjectCollection());
			var csProjItems = csProj.Items.Select(i => i.UnevaluatedInclude);

			var wrongAnswersOrSolution = GetOrderedFileNames(csProjItems, ProjectExerciseBlock.IsAnyWrongAnswerOrAnySolution);

			if (wrongAnswersOrSolution.Any())
				ReportSlideError(slide, $"Student's csproj has 'wrong answer' and/or solution items ({string.Join(", ", wrongAnswersOrSolution)})");
		}
	}
}