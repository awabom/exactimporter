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

		public CompareAction CompareAction { get; set; }
		public NonExisting NonExisting { get; set; }
	}

	public class ImporterResult
	{
		public List<FileResult> FileResults { get; set; }
	}

	public enum CompareAction
	{
		None = 0,
		Attributes = 1,
		Contents = 2,
		AttributesAndContents = Attributes | Contents
	}

	public enum FileResultCode
	{
		Matched,
		MissingInDestination,
		ContentsDifferent,
		AttributesDifferent
	}

	public class FileResult
	{
		public bool Failure { get; set; }
		public string FileSourcePath { get; set; }
		public string FileDestinationPath { get; set; }
		public FileActionTaken ActionTaken { get; set; }
		public FileResultCode ResultCode { get; set; }

		public string ToDisplayString()
		{
			return FileSourcePath + " " + ActionTaken + ", " + ResultCode + " " + FileDestinationPath + (Failure ? "(failure!)" : " (ok)");
		}
	}

	public enum FileActionTaken
	{
		None,
		Existed,
		Copied
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
			if (config == null) throw new ArgumentNullException(nameof(config));
			_config = config;
		}


		private FileResult ImportVerify(FileInfo srcInfo, string dstPath)
		{
			string srcPath = srcInfo.FullName;
			
			FileActionTaken actionTaken;

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
							ActionTaken = FileActionTaken.None,
							ResultCode = FileResultCode.MissingInDestination
						};
					case NonExisting.Copy:
						File.Copy(srcPath, dstPath);
						actionTaken = FileActionTaken.Copied;
						break;
					case NonExisting.Skip:
						// Return a non-failure, but 'missing' status
						return new FileResult
						{
							Failure = false,
							FileSourcePath = srcPath,
							FileDestinationPath = dstPath,
							ActionTaken = FileActionTaken.None,
							ResultCode = FileResultCode.MissingInDestination
						};
					default:
						throw new NotImplementedException("NonExisting." + _config.NonExisting + " not implemented");
				}
			}
			else
			{
				actionTaken = FileActionTaken.Existed;
			}

			if ((_config.CompareAction & CompareAction.Attributes) != 0 && !FileAttributesEquals(srcInfo, dstPath))
			{
				return new FileResult
				{
					Failure = true,
					FileSourcePath = srcPath,
					FileDestinationPath = dstPath,
					ActionTaken = actionTaken,
					ResultCode = FileResultCode.AttributesDifferent
				};
			}

			// File exists (existed or was just copied), verify contents (if enabled)
			if ((_config.CompareAction & CompareAction.Contents) != 0 && !FileContentsEquals(srcPath, dstPath))
			{
				return new FileResult
				{
					Failure = true,
					FileSourcePath = srcPath,
					FileDestinationPath = dstPath,
					ActionTaken = actionTaken,
					ResultCode = FileResultCode.ContentsDifferent
				};
			}

			return new FileResult
			{
				Failure = false,
				FileSourcePath = srcPath,
				FileDestinationPath = dstPath,
				ActionTaken = actionTaken,
				ResultCode = FileResultCode.Matched
			};
		}

		private bool FileAttributesEquals(FileInfo infoA, string pathB)
		{
			var infoB = new FileInfo(pathB);
			return infoA.Length == infoB.Length;
		}

		private bool FileContentsEquals(string pathA, string pathB)
		{
			const int ChunkSize = 1024 * 1024 * 10; // 10 MB
			byte[] bufferA = new byte[ChunkSize];
			byte[] bufferB = new byte[ChunkSize];

			using (Stream a = new FileStream(pathA, FileMode.Open, FileAccess.Read))
			using (Stream b = new FileStream(pathB, FileMode.Open, FileAccess.Read))
			{
				for (;;)
				{
					// Read the same amount of bytes from both files
					int readA = a.Read(bufferA, 0, bufferA.Length);
					int readB = b.Read(bufferB, 0, bufferB.Length);

					// Not same byte count read - differs
					if (readA != readB)
						return false;

					// End of file without finding differences - matching!
					if (readA == 0)
						return true;

					// Contents don't match
					if (!ArrayEquals(bufferA, bufferB, readA))
						return false;
				}
			}
		}

		private bool ArrayEquals(byte[] a, byte[] b, int compareLength)
		{
			for (int i=0; i < compareLength; i++)
			{
				if (a[i] != b[i])
					return false;
			}

			return true;
		}

		public ImporterResult Import()
		{
			ImporterResult result = new ImporterResult { FileResults = new List<FileResult>() };

			foreach (DirectoryInfo srcDir in _config.SourcePaths.Select(x => new DirectoryInfo(x)))
			{
				foreach (FileInfo srcInfo in srcDir.GetFiles("*", SearchOption.AllDirectories).Where(x => _config.ImportExtensionsWithDot.Contains(x.Extension.ToLowerInvariant())))
				{
					string dstPath = GetDestinationPath(srcInfo);

					FileResult fileResult = ImportVerify(srcInfo, dstPath);
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
