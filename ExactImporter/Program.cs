using System;
using System.Collections.Generic;
using System.Linq;
using ExactImporterLib;

namespace ExactImporter
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length <= 1)
			{
				Console.Error.WriteLine("Usage: <TargetPath> <SourcePath 1> <SourcePath 2> ... <SourcePath N>");
				return;
			}

			string targetPath = args[0];
			string[] sourcePaths = args.Skip(1).ToArray();

			string[] extensionsWithDot = { ".jpg", ".rw2" /* panasonic raw */, ".mp4", ".avi", ".mov", ".cr2" /* canon raw */, ".log", ".mlv", ".idx" /* magic lantern movie log,raw,index */ };

			ImporterConfig config = new ImporterConfig
			{
				ImportExtensionsWithDot = new HashSet<string>(extensionsWithDot),
				TargetPath = targetPath,
				SourcePaths = new List<string>(sourcePaths),
				NonExisting = NonExisting.Copy
			};
			Importer importer = new Importer(config);
			var result = importer.Import();

			// Output all failures
			bool hasFailure = false;
			foreach (var fileResult in result.FileResults.Where(x => x.Failure))
			{
				hasFailure = true;
				Console.Error.WriteLine(fileResult.ToDisplayString());
			}

			Console.WriteLine(hasFailure ? "Finished WITH ERRORS" : "Finished ALL OK");

			Environment.Exit(hasFailure ? 1 : 0);
		}
	}
}
