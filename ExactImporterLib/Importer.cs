using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace ExactImporterLib
{
	public class ImporterConfig
	{
		public string TargetPath { get; set; }
		public List<string> SourcePaths { get; set; }
		public HashSet<string> ImportExtensionsWithDot { get; set; }

		public NonExisting NonExisting { get; set; }
	}

	public class ImporterResult
	{
		public List<FileResult> FileResults { get; set; }
	}

	public enum FileResultCode
	{
		Matched,
		MissingInDestination,
		ContentsDifferent
	}

	public class FileResult
	{
		public bool Failure { get; set; }
		public string FileSourcePath { get; set; }
		public string FileDestinationPath { get; set; }
		public FileResultCode ResultCode { get; set; }

		public string ToDisplayString()
		{
			return FileSourcePath + " " + ResultCode + " " + FileDestinationPath + (Failure ? "(failure!)" : " (ok)");
		}
	}

	public enum NonExisting
	{
		Fail,
		Copy,
		Skip
	}

	public class Importer
	{
		ImporterConfig _config;

		public Importer(ImporterConfig config)
		{
			if (config == null) throw new ArgumentNullException("config");
			_config = config;
		}


		private FileResult ImportVerify(string srcPath, string dstPath)
		{
			// If destination does not exist - handle according to config.NonExisting mode
			if (!File.Exists(dstPath))
			{
				switch (_config.NonExisting)
				{
					case NonExisting.Fail:
						return new FileResult
						{
							Failure = true,
							FileSourcePath = srcPath,
							FileDestinationPath = dstPath,
							ResultCode = FileResultCode.MissingInDestination
						};
					case NonExisting.Copy:
						File.Copy(srcPath, dstPath);
						break;
					case NonExisting.Skip:
						// Return a non-failure, but 'missing' status
						return new FileResult
						{
							Failure = false,
							FileSourcePath = srcPath,
							FileDestinationPath = dstPath,
							ResultCode = FileResultCode.MissingInDestination
						};
					default:
						throw new NotImplementedException("NonExisting." + _config.NonExisting + " not implemented");
				}

			}

			// File exists (existed or was just copied), verify contents
			var srcBytes = File.ReadAllBytes(srcPath);
			var dstBytes = File.ReadAllBytes(dstPath);

			if (!ArrayEquals(srcBytes, dstBytes))
			{
				return new FileResult
				{
					Failure = true,
					FileSourcePath = srcPath,
					FileDestinationPath = dstPath,
					ResultCode = FileResultCode.ContentsDifferent
				};
			}

			return new FileResult
			{
				Failure = false,
				FileSourcePath = srcPath,
				FileDestinationPath = dstPath,
				ResultCode = FileResultCode.Matched
			};
		}

		private bool ArrayEquals(byte[] a, byte[] b)
		{
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;

			return a.SequenceEqual(b);
		}

		public ImporterResult Import()
		{
			ImporterResult result = new ImporterResult { FileResults = new List<FileResult>() };

			foreach (DirectoryInfo srcDir in _config.SourcePaths.Select(x => new DirectoryInfo(x)))
			{
				foreach (FileInfo fileInfo in srcDir.GetFiles("*", SearchOption.AllDirectories).Where(x => _config.ImportExtensionsWithDot.Contains(x.Extension.ToLowerInvariant())))
				{
					string srcPath = fileInfo.FullName;
					string dstPath = GetDestinationPath(fileInfo);

					FileResult fileResult = ImportVerify(srcPath, dstPath);
					Console.WriteLine(fileResult.ToDisplayString());
					result.FileResults.Add(fileResult);
				}
			}

			return result;
		}

		private string GetDestinationPath(FileInfo fileInfo)
		{
			string subDirName = fileInfo.CreationTime.ToString("yyyy-MM-dd");
			string subDirPath = Path.Combine(_config.TargetPath, subDirName);
			Directory.CreateDirectory(subDirPath);
			return Path.Combine(subDirPath, fileInfo.Name);
		}
	}
}
