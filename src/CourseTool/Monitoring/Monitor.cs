﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace uLearn.CourseTool.Monitoring
{
	internal class Monitor
	{
		private readonly PreviewHttpServer server;
		private readonly string courseDir;

		public static void Start(string homeDir, string courseId)
		{
			// ReSharper disable once ObjectCreationAsStatement
			new Monitor(homeDir, courseId);
		}

		private Monitor(string homeDir, string courseId)
		{
			courseDir = Path.Combine(homeDir, courseId);
			server = new PreviewHttpServer(courseDir, Path.Combine(homeDir, "html"), 1337);
			server.Start();
			StartWatchingCourseDir();
			Console.WriteLine($"Started monitoring {courseDir}");
			OpenInBrowser();
			while (true)
			{
				var key = Console.ReadKey(true).Key;
				if (key == ConsoleKey.Q)
					break;
				if (key == ConsoleKey.O)
					OpenInBrowser();
				if (key == ConsoleKey.R)
					ReloadCourse();
				else
					Console.WriteLine(@"Press 'Q' to exit. Press 'O' to open course in browser");
			}
		}

		private void ReloadCourse()
		{
			server.ForceReloadCourse();
		}

		private void StartWatchingCourseDir()
		{
			var fileWatcher = new FileSystemWatcher(courseDir) { IncludeSubdirectories = true };
			fileWatcher.Changed += FileWatcherOnChanged;
			fileWatcher.Renamed += FileWatcherOnChanged;
			fileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
			fileWatcher.EnableRaisingEvents = true;
		}

		private void OpenInBrowser()
		{
			var lastChangedHtml = server.FindLastChangedSlideHtmlPath() ?? "001.html";
			Process.Start(@"http://localhost:1337/" + lastChangedHtml);
		}

		private void FileWatcherOnChanged(object sender, FileSystemEventArgs args)
		{
			var extensions = new[] { ".cs", ".gif", ".bmp", ".sln", ".xml", ".png", ".csproj", ".txt", ".md" };
			var monitoredExtension = extensions.Any(ext => args.Name.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase));
			var insideCheckingDir = args.FullPath.ToLower().Contains("\\checking\\");
			if (!insideCheckingDir && monitoredExtension)
			{
				Console.WriteLine($"{DateTime.Now:T} {args.Name} was {args.ChangeType.ToString().ToLower()}.");
				server.MarkCourseAsChanged();
			}
		}
	}
}